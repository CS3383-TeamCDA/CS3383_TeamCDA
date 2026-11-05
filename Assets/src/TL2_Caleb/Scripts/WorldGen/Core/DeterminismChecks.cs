using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Engine-free checks, shared by the Unity EditMode test and the command-line runner.
    /// RunAll returns a list of failure messages; empty means everything passed.
    /// </summary>
    public static class DeterminismChecks
    {
        // Update deliberately if you change generation rules; a surprise change means determinism broke.
        public static readonly ulong GoldenStreetHash = 0xDFAEC2B289CC5617UL;
        public static readonly ulong GoldenRegionHash = 0xC6320044955F4937UL;
        public static readonly ulong GoldenStreetsHash = 0xAEE05FD844B15C47UL;

        public static BiomeRules[] TestBiomes()
        {
            var b = new BiomeRules[WorldConstants.BiomeCount];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = BiomeRules.CreateDefault("Biome" + i);
                b[i].EnemyKindWeights = new[] { 3, 2, 1 };
                b[i].Buildings[0].VariantCount = 3;
            }
            return b;
        }

        public static StreetRequest Req(int a, int b, int chunks, int biome, BuildingFlags allowed, int tier, params BuildingType[] required)
        {
            return new StreetRequest
            {
                Id = StableId.ForStreet(new Int2(a, 0), new Int2(b, 1)),
                ChunkCount = chunks, Biome = biome, Allowed = allowed,
                DifficultyTier = tier, Required = required.Length == 0 ? null : required
            };
        }

        public static List<string> RunAll()
        {
            var f = new List<string>();
            RngBasics(f); IdSymmetry(f); SameInputsSameOutput(f); OrderIndependence(f);
            ChunkIndependence(f); Constraints(f); RequiredOverflow(f); Distribution(f); Golden(f);
            TerrainAndDualGrid(f); WindowAndColliders(f); RegionChecks(f); StreetNetworkChecks(f);
            return f;
        }

        static void RngBasics(List<string> f)
        {
            var a = DeterministicRng.Create(1, new StableId(5), Salt.Placement);
            var b = DeterministicRng.Create(1, new StableId(5), Salt.Placement);
            var c = DeterministicRng.Create(1, new StableId(6), Salt.Placement);
            var d = DeterministicRng.Create(1, new StableId(5), Salt.Enemies);
            bool same = true, diffId = false, diffSalt = false;
            for (int i = 0; i < 16; i++)
            {
                ulong x = a.NextULong();
                if (x != b.NextULong()) same = false;
                if (x != c.NextULong()) diffId = true;
                if (x != d.NextULong()) diffSalt = true;
            }
            if (!same) f.Add("Rng: identical inputs gave different streams");
            if (!diffId) f.Add("Rng: different ids gave identical streams");
            if (!diffSalt) f.Add("Rng: different salts gave identical streams");
        }

        static void IdSymmetry(List<string> f)
        {
            var p = new Int2(3, -2); var q = new Int2(-7, 9);
            if (!StableId.ForStreet(p, q).Equals(StableId.ForStreet(q, p))) f.Add("StreetId depends on endpoint order");
            if (StableId.ForStreet(p, q).Equals(StableId.ForStreet(p, new Int2(-7, 10)))) f.Add("StreetId collision on adjacent streets");
            var x = StableId.ForStreet(p, q); var y = StableId.ForStreet(q, new Int2(0, 0));
            if (!StableId.ForPair(x, y).Equals(StableId.ForPair(y, x))) f.Add("PairId depends on order");
        }

        static void SameInputsSameOutput(List<string> f)
        {
            var biomes = TestBiomes();
            var r = Req(1, 2, 20, 0, BuildingFlags.All, 3, BuildingType.Arcade);
            ulong h1 = StreetGenerator.Generate(42, r, biomes[0]).ContentHash();
            ulong h2 = StreetGenerator.Generate(42, r, biomes[0]).ContentHash();
            ulong h3 = StreetGenerator.Generate(43, r, biomes[0]).ContentHash();
            if (h1 != h2) f.Add("Same seed+request produced different streets");
            if (h1 == h3) f.Add("Different seeds produced identical streets");
        }

        static void OrderIndependence(List<string> f)
        {
            var biomes = TestBiomes();
            var reqs = new List<StreetRequest>();
            for (int i = 0; i < 40; i++) reqs.Add(Req(i, i + 100, 12 + i % 20, i % 5, BuildingFlags.All, i % 7));

            var forward = new LevelService(777, biomes);
            var hashesF = new ulong[reqs.Count];
            for (int i = 0; i < reqs.Count; i++) hashesF[i] = forward.GetStreet(reqs[i]).ContentHash();

            var back = new LevelService(777, biomes);
            for (int i = reqs.Count - 1; i >= 0; i--)
                if (back.GetStreet(reqs[i]).ContentHash() != hashesF[i]) f.Add("Street " + i + " differs when generated in reverse order");

            // Generate everything again after unrelated streets: a stateful RNG would break here.
            var shuffled = new LevelService(777, biomes);
            for (int i = 0; i < reqs.Count; i += 3) shuffled.GetStreet(reqs[i]);
            for (int i = 0; i < reqs.Count; i++)
                if (shuffled.GetStreet(reqs[i]).ContentHash() != hashesF[i]) f.Add("Street " + i + " differs after unrelated streets were generated");
        }

        static void ChunkIndependence(List<string> f)
        {
            var biomes = TestBiomes();
            var r = Req(5, 9, 30, 2, BuildingFlags.All, 4, BuildingType.Arcade, BuildingType.Arcade);
            StreetLayout s = StreetGenerator.Generate(99, r, biomes[2]);
            for (int i = 0; i < s.ChunkCount; i += 1)
            {
                ChunkData alone = StreetGenerator.GenerateChunk(99, r.Id, i, s.ChunkCount, s.Chunks[i].Building, biomes[2], 4);
                if (StreetLayout.ChunkHash(alone) != StreetLayout.ChunkHash(s.Chunks[i]))
                { f.Add("Chunk " + i + " rebuilt alone differs from the street's copy"); break; }
            }
        }

        static void Constraints(List<string> f)
        {
            var biomes = TestBiomes();
            for (int seed = 0; seed < 300; seed++)
                for (int k = 0; k < 8; k++)
                {
                    int chunks = WorldConstants.MinStreetChunks + k * 3;
                    int biome = (seed + k) % biomes.Length;
                    var r = Req(seed, k, chunks, biome, BuildingFlags.All, (seed + k) % 12, BuildingType.Arcade);
                    BiomeRules rules = biomes[biome];
                    StreetLayout s = StreetGenerator.Generate((ulong)seed, r, rules);
                    var counts = new Dictionary<BuildingType, int>();
                    for (int i = 0; i < s.ChunkCount; i++)
                    {
                        ChunkData c = s.Chunks[i];
                        if (c.IsSafe && (c.Building != BuildingType.None || c.Enemies.Length != 0))
                        { f.Add("Safe end chunk has content (seed " + seed + ")"); return; }
                        if (c.Enemies.Length > rules.EnemyHardCap) { f.Add("Enemy count over hard cap"); return; }
                        int anchored = 0;
                        for (int e = 0; e < c.Enemies.Length; e++) if (c.Enemies[e].OnBuilding) anchored++;
                        BuildingRule rule = rules.FindRule(c.Building);
                        if (anchored > (rule == null ? 0 : rule.EnemyAnchors)) { f.Add("More anchored enemies than anchors"); return; }
                        if (c.Flipped && rule != null && !rule.AllowFlip) { f.Add("Flipped a no-flip building"); return; }
                        int v; counts.TryGetValue(c.Building, out v); counts[c.Building] = v + 1;
                    }
                    int arcades; counts.TryGetValue(BuildingType.Arcade, out arcades);
                    if (arcades != 1 || s.DroppedRequired != 0) { f.Add("Required arcade missing or duplicated (seed " + seed + ")"); return; }
                    foreach (var rule in rules.Buildings)
                    {
                        if (rule.Type == BuildingType.Arcade || !rule.Type.IsSpecial()) continue;
                        int n; counts.TryGetValue(rule.Type, out n);
                        if (n > rule.MaxPerStreet) { f.Add(rule.Type + " exceeds MaxPerStreet"); return; }
                    }
                }
        }

        static void RequiredOverflow(List<string> f)
        {
            var biomes = TestBiomes();
            int usable = WorldConstants.MinStreetChunks - 2 * WorldConstants.EndSafeChunks;
            var req = new BuildingType[usable + 2];
            for (int i = 0; i < req.Length; i++) req[i] = BuildingType.Arcade;
            var r = Req(1, 2, WorldConstants.MinStreetChunks, 0, BuildingFlags.All, 0, req);
            StreetLayout s = StreetGenerator.Generate(5, r, biomes[0]);
            if (s.DroppedRequired != 2) f.Add("Expected 2 dropped required buildings, got " + s.DroppedRequired);
        }

        static void Distribution(List<string> f)
        {
            var biomes = TestBiomes();
            int withPizza = 0, total = 20000, noFlag = 0;
            for (int i = 0; i < total; i++)
            {
                var r = Req(i, i + 1, 20, 0, BuildingFlags.PizzaParlor, 0);
                StreetLayout s = StreetGenerator.Generate(2024, r, biomes[0]);
                bool has = false;
                foreach (var c in s.Chunks) if (c.Building == BuildingType.PizzaParlor) has = true;
                if (has) withPizza++;
                var r2 = Req(i, i + 1, 20, 0, BuildingFlags.None, 0);
                foreach (var c in StreetGenerator.Generate(2024, r2, biomes[0]).Chunks) if (c.Building.IsSpecial()) noFlag++;
            }
            double pct = 100.0 * withPizza / total;
            if (pct < 17.0 || pct > 23.0) f.Add("Pizza parlor rate " + pct.ToString("F1") + "% (expected about 20%)");
            if (noFlag != 0) f.Add("Special buildings appeared without their flag: " + noFlag);
        }


        // ---- Phase 3: terrain + dual grid ----
        static void TerrainAndDualGrid(List<string> f)
        {
            var biomes = TestBiomes();
            int w = WorldConstants.ChunkWidthCells;
            for (int seed = 0; seed < 60; seed++)
            {
                var r = Req(seed, seed + 1, 20, seed % 5, BuildingFlags.All, 0, BuildingType.Arcade);
                StreetLayout layout = StreetGenerator.Generate((ulong)seed, r, biomes[seed % 5]);
                var t1 = new StreetTerrain((ulong)seed, layout, true);
                var t2 = new StreetTerrain((ulong)seed, layout, true);
                if (t1.ContentHash() != t2.ContentHash()) { f.Add("Terrain not deterministic"); return; }

                // bumps only in building-free, non-safe chunks, and within the height limit
                for (int x = 0; x < t1.WidthCells; x++)
                {
                    int hgt = t1.HeightAt(x);
                    if (hgt > WorldConstants.MaxBumpHeight) { f.Add("Bump taller than MaxBumpHeight"); return; }
                    if (hgt > 0)
                    {
                        ChunkData c = layout.Chunks[x / w];
                        if (c.IsSafe || c.Building != BuildingType.None) { f.Add("Bump under a building or safe chunk"); return; }
                    }
                }

                // dual-grid consistency: each data cell must agree with the corner bit of its 4 incident tiles
                for (int x = -2; x < t1.WidthCells + 2; x++)
                    for (int y = -WorldConstants.GroundDepthCells; y <= WorldConstants.MaxBumpHeight; y++)
                    {
                        bool filled = t1.IsFilled(x, y);
                        if (((DualGrid.MaskAt(t1, x + 1, y + 1) & DualGrid.BL) != 0) != filled ||
                            ((DualGrid.MaskAt(t1, x, y + 1) & DualGrid.BR) != 0) != filled ||
                            ((DualGrid.MaskAt(t1, x + 1, y) & DualGrid.TL) != 0) != filled ||
                            ((DualGrid.MaskAt(t1, x, y) & DualGrid.TR) != 0) != filled)
                        { f.Add("Dual-grid corner mismatch at cell " + x + "," + y); return; }
                    }

                // a flat street must only need the 'top edge' (12) and 'solid' (15) tiles
                var flat = new StreetTerrain((ulong)seed, layout, false);
                for (int x = 0; x <= flat.WidthCells; x++)
                {
                    if (DualGrid.MaskAt(flat, x, 0) != (DualGrid.BL | DualGrid.BR)) { f.Add("Flat street top row is not mask 12"); return; }
                    if (DualGrid.MaskAt(flat, x, -1) != 15) { f.Add("Flat street body row is not mask 15"); return; }
                }
            }
        }


        // ---- Phase 4: streaming window + colliders ----
        static void WindowAndColliders(List<string> f)
        {
            if (ChunkWindow.ChunkIndexAt(-50f, 10) != 0) f.Add("ChunkIndexAt must clamp low");
            if (ChunkWindow.ChunkIndexAt(99999f, 10) != 9) f.Add("ChunkIndexAt must clamp high");
            if (ChunkWindow.ChunkIndexAt(WorldConstants.ChunkWidthCells, 10) != 1) f.Add("ChunkIndexAt boundary wrong");
            for (int c = 0; c < 12; c++)
                for (int i = 0; i < 12; i++)
                    for (int r = 1; r <= 3; r++)
                    {
                        if (ChunkWindow.ShouldLoad(i, c, r) && ChunkWindow.ShouldUnload(i, c, r)) { f.Add("A chunk is both loaded and unloaded"); return; }
                        bool keep = ChunkWindow.ShouldLoad(i, c, r) || !ChunkWindow.ShouldUnload(i, c, r);
                        if (!keep && System.Math.Abs(i - c) <= r) { f.Add("Hysteresis broken"); return; }
                    }

            var biomes = TestBiomes();
            int w = WorldConstants.ChunkWidthCells;
            for (int seed = 0; seed < 40; seed++)
            {
                var r = Req(seed, seed + 3, 16, seed % 5, BuildingFlags.All, 0, BuildingType.Arcade);
                StreetLayout layout = StreetGenerator.Generate((ulong)seed, r, biomes[seed % 5]);
                var t = new StreetTerrain((ulong)seed, layout, true);
                for (int ci = 0; ci < layout.ChunkCount; ci++)
                {
                    List<ColliderBox> boxes = TerrainColliders.ForChunk(t, ci);
                    foreach (ColliderBox b in boxes)
                        if (b.X0 < 0 || b.X1 > w) { f.Add("Collider box leaves its chunk"); return; }
                    for (int lx = 0; lx < w; lx++)
                        for (int y = -WorldConstants.SlabDepthCells; y <= WorldConstants.MaxBumpHeight + 1; y++)
                        {
                            bool covered = false;
                            foreach (ColliderBox b in boxes)
                                if (lx >= b.X0 && lx < b.X1 && y >= b.Y0 && y < b.Y1) { covered = true; break; }
                            if (covered != t.IsFilled(ci * w + lx, y))
                            { f.Add("Colliders do not match terrain at chunk " + ci + " cell " + lx + "," + y); return; }
                        }
                }
            }
        }


        // ---- Milestone 2: region map ----
        static string EdgeKey(BorderEdge e) { return e.From + ">" + e.To; }

        static void RegionChecks(List<string> f)
        {
            int r = WorldConstants.RegionSizeNodes;
            // 1) nearest site is always inside the 3x3 cells (compare with a 5x5 brute force)
            for (ulong seed = 1; seed <= 3; seed++)
            {
                var map = new RegionMap(seed);
                var rng = new DeterministicRng(seed * 7919);
                for (int i = 0; i < 3000; i++)
                {
                    var n = new Int2(rng.Range(-300, 300), rng.Range(-300, 300));
                    int cx = RegionMap.FloorDiv(n.X, r), cy = RegionMap.FloorDiv(n.Y, r);
                    Int2 best = new Int2(cx, cy); long bd = long.MaxValue;
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            var cell = new Int2(cx + dx, cy + dy); Int2 st = map.SiteOf(cell);
                            long ddx = n.X - st.X, ddy = n.Y - st.Y, d = ddx * ddx + ddy * ddy;
                            if (d < bd || (d == bd && cell.CompareTo(best) < 0)) { bd = d; best = cell; }
                        }
                    if (!map.RegionAt(n).Equals(best)) { f.Add("RegionAt disagrees with 5x5 brute force at " + n); return; }
                }
                for (int cy = -3; cy <= 3; cy++)
                    for (int cx = -3; cx <= 3; cx++)
                        if (!map.RegionAt(map.SiteOf(new Int2(cx, cy))).Equals(new Int2(cx, cy))) { f.Add("A site is not inside its own region"); return; }
            }

            // 2) deterministic + order independent + seed sensitive
            ulong h1 = new RegionMap(99).WindowHash(4);
            var m2 = new RegionMap(99);
            for (int cy = 4; cy >= -4; cy--) for (int cx = 4; cx >= -4; cx--)
                foreach (Int2 nb in m2.GetRegion(new Int2(cx, cy)).Neighbors) m2.GetLink(nb, new Int2(cx, cy));
            if (h1 != m2.WindowHash(4)) f.Add("Region map depends on query order");
            if (h1 == new RegionMap(100).WindowHash(4)) f.Add("Different seeds gave identical region maps");

            // 3-7) structure over several seeds
            int pairs = 0, subwayOnly = 0;
            for (ulong seed = 11; seed <= 14; seed++)
            {
                var map = new RegionMap(seed);
                const int W = 5;
                for (int cy = -W; cy <= W; cy++)
                    for (int cx = -W; cx <= W; cx++)
                    {
                        var id = new Int2(cx, cy);
                        RegionInfo a = map.GetRegion(id);
                        if (a.Neighbors.Length < 3) { f.Add("Region " + id + " has only " + a.Neighbors.Length + " neighbours"); return; }
                        if (a.NodeCount < 60 || a.NodeCount > 260) { f.Add("Region " + id + " has odd size " + a.NodeCount); return; }
                        foreach (Int2 nb in a.Neighbors)
                        {
                            // border symmetry: B's edges into A are exactly A's edges into B, reversed
                            RegionInfo b = map.GetRegion(nb);
                            var fwd = new HashSet<string>(); foreach (BorderEdge e in a.BordersTo(nb)) fwd.Add(EdgeKey(e.Reversed()));
                            var back = new HashSet<string>(); foreach (BorderEdge e in b.BordersTo(id)) back.Add(EdgeKey(e));
                            if (!fwd.SetEquals(back)) { f.Add("Border edges are not symmetric between " + id + " and " + nb); return; }

                            if (id.CompareTo(nb) > 0) continue;
                            RegionLink l = map.GetLink(id, nb);
                            RegionLink l2 = map.GetLink(nb, id);
                            if (l == null || !ReferenceEquals(l, l2)) { f.Add("GetLink is not symmetric"); return; }
                            pairs++;
                            if (!l.Connected) { f.Add("Adjacent regions " + id + "," + nb + " are not connected"); return; }
                            if (l.Surface.Length > 3) { f.Add("More than 3 surface links"); return; }
                            if (l.Surface.Length == 0) subwayOnly++;
                            if (l.Surface.Length > 0 && l.HasSubway) { f.Add("Subway link added although surface links exist"); return; }
                            for (int i = 0; i < l.Surface.Length; i++)
                            {
                                BorderEdge e = l.Surface[i];
                                if (!map.RegionAt(e.From).Equals(l.A) || !map.RegionAt(e.To).Equals(l.B) ||
                                    System.Math.Abs(e.From.X - e.To.X) + System.Math.Abs(e.From.Y - e.To.Y) != 1)
                                { f.Add("Surface link is not a border edge A->B"); return; }
                                for (int j = i + 1; j < l.Surface.Length; j++)
                                    if (System.Math.Abs(e.From.X - l.Surface[j].From.X) + System.Math.Abs(e.From.Y - l.Surface[j].From.Y) < WorldConstants.MinConnectionSpacing)
                                    { f.Add("Surface links closer than the minimum spacing"); return; }
                            }
                            if (l.HasSubway && (!map.RegionAt(l.StationA).Equals(l.A) || !map.RegionAt(l.StationB).Equals(l.B)))
                            { f.Add("Subway stations are not inside their regions"); return; }
                        }
                    }

                // the whole window is one connected world (BFS over adjacency inside the window)
                var seen = new HashSet<Int2>(); var queue = new Queue<Int2>();
                queue.Enqueue(new Int2(0, 0)); seen.Add(new Int2(0, 0));
                while (queue.Count > 0)
                {
                    Int2 cur = queue.Dequeue();
                    foreach (Int2 nb in map.GetRegion(cur).Neighbors)
                        if (System.Math.Abs(nb.X) <= W && System.Math.Abs(nb.Y) <= W && seen.Add(nb)) queue.Enqueue(nb);
                }
                if (seen.Count != (2 * W + 1) * (2 * W + 1)) { f.Add("Region window is not connected (" + seen.Count + " reachable)"); return; }
            }
            double subPct = pairs == 0 ? 0 : 100.0 * subwayOnly / pairs;
            if (subPct < 15.0 || subPct > 35.0) f.Add("Subway-only pairs are " + subPct.ToString("F1") + "% (expected about 25%)");

            if (GoldenRegionHash != 0UL && new RegionMap(0xBEEFUL).WindowHash(5) != GoldenRegionHash)
                f.Add("Golden region hash changed: got " + new RegionMap(0xBEEFUL).WindowHash(5).ToString("X16"));
        }

        public static ulong GoldenRegionValue() { return new RegionMap(0xBEEFUL).WindowHash(5); }


        // ---- Phase 7: street networks inside regions ----
        static void StreetNetworkChecks(List<string> f)
        {
            const int W = 3;
            long borderTotal = 0, borderIn = 0, interiorTotal = 0, interiorIn = 0, nodesSum = 0, deadSum = 0;
            for (ulong seed = 31; seed <= 33; seed++)
            {
                var map = new RegionMap(seed);
                for (int cy = -W; cy <= W; cy++)
                    for (int cx = -W; cx <= W; cx++)
                    {
                        var id = new Int2(cx, cy);
                        RegionStreets st = map.GetStreets(id);
                        string where = " (seed " + seed + ", region " + id + ")";
                        var nodeSet = new HashSet<Int2>(st.Nodes);
                        if (nodeSet.Count != st.Nodes.Length) { f.Add("Duplicate network nodes" + where); return; }
                        if (st.Unreachable != 0) { f.Add("Unreachable terminals" + where); return; }
                        if (st.ArcadeNodes.Length < 2 || st.ArcadeNodes.Length > 5) { f.Add("Arcade count " + st.ArcadeNodes.Length + where); return; }

                        for (int i = 0; i < st.ArcadeNodes.Length; i++)
                        {
                            Int2 a = st.ArcadeNodes[i];
                            if (!nodeSet.Contains(a)) { f.Add("Arcade node is not on a street" + where); return; }
                            if (!IsInterior(map, id, a)) { f.Add("Arcade on the region border" + where); return; }
                            for (int j = i + 1; j < st.ArcadeNodes.Length; j++)
                                if (System.Math.Abs(a.X - st.ArcadeNodes[j].X) + System.Math.Abs(a.Y - st.ArcadeNodes[j].Y) < 3) { f.Add("Arcades closer than 3 nodes" + where); return; }
                        }
                        foreach (Int2 n in st.LinkNodes) if (!nodeSet.Contains(n)) { f.Add("Link node is not on a street" + where); return; }
                        foreach (Int2 n in st.SubwayNodes) if (!nodeSet.Contains(n)) { f.Add("Subway station is not on a street" + where); return; }

                        // every surface link of this region appears as an external edge here AND in the neighbour's list
                        var keys = new HashSet<string>();
                        foreach (StreetEdge e in st.Edges)
                        {
                            if (!keys.Add(e.A + ">" + e.B)) { f.Add("Duplicate street edge" + where); return; }
                            if (e.A.CompareTo(e.B) >= 0) { f.Add("Street edge not canonical" + where); return; }
                            if (System.Math.Abs(e.A.X - e.B.X) + System.Math.Abs(e.A.Y - e.B.Y) != 1) { f.Add("Street edge is not between adjacent nodes" + where); return; }
                            bool aIn = map.RegionAt(e.A).Equals(id), bIn = map.RegionAt(e.B).Equals(id);
                            if (!e.External && !(aIn && bIn)) { f.Add("Internal edge leaves the region" + where); return; }
                            if (e.External)
                            {
                                if (aIn == bIn) { f.Add("External edge does not cross the border" + where); return; }
                                Int2 other = aIn ? e.B : e.A;
                                bool found = false;
                                foreach (StreetEdge oe in map.GetStreets(map.RegionAt(other)).Edges)
                                    if (oe.A.Equals(e.A) && oe.B.Equals(e.B) && oe.External) { found = true; break; }
                                if (!found) { f.Add("Link edge missing from the neighbouring region" + where); return; }
                            }
                        }

                        // the network inside the region is connected
                        var adj = new Dictionary<Int2, List<Int2>>();
                        foreach (StreetEdge e in st.Edges)
                        {
                            if (!nodeSet.Contains(e.A) || !nodeSet.Contains(e.B)) continue;
                            if (!adj.ContainsKey(e.A)) adj[e.A] = new List<Int2>();
                            if (!adj.ContainsKey(e.B)) adj[e.B] = new List<Int2>();
                            adj[e.A].Add(e.B); adj[e.B].Add(e.A);
                        }
                        var seen = new HashSet<Int2>(); var queue = new Queue<Int2>();
                        queue.Enqueue(st.Nodes[0]); seen.Add(st.Nodes[0]);
                        while (queue.Count > 0)
                        {
                            Int2 cur = queue.Dequeue(); List<Int2> list;
                            if (adj.TryGetValue(cur, out list)) foreach (Int2 nb in list) if (seen.Add(nb)) queue.Enqueue(nb);
                        }
                        if (seen.Count != st.Nodes.Length) { f.Add("Street network is not connected (" + seen.Count + "/" + st.Nodes.Length + ")" + where); return; }

                        // density stats: streets should favour the middle of a region
                        int r = WorldConstants.RegionSizeNodes;
                        for (int y = (cy - 1) * r; y < (cy + 2) * r; y++)
                            for (int x = (cx - 1) * r; x < (cx + 2) * r; x++)
                            {
                                var n = new Int2(x, y);
                                if (!map.RegionAt(n).Equals(id)) continue;
                                bool inNet = nodeSet.Contains(n);
                                if (IsInterior(map, id, n)) { interiorTotal++; if (inNet) interiorIn++; }
                                else { borderTotal++; if (inNet) borderIn++; }
                            }
                        nodesSum += st.Nodes.Length; deadSum += st.DeadEnds;
                    }
            }
            double borderFrac = (double)borderIn / borderTotal, interiorFrac = (double)interiorIn / interiorTotal;
            if (borderFrac >= interiorFrac * 0.8) f.Add("Streets do not favour the interior: border " + borderFrac.ToString("F2") + " vs interior " + interiorFrac.ToString("F2"));
            if (deadSum > nodesSum * 0.15) f.Add("Too many dead ends: " + deadSum + " of " + nodesSum + " nodes");

            // order independence of the whole street layer
            ulong h1 = new RegionMap(77).StreetsHash(3);
            var m2 = new RegionMap(77);
            for (int cy = 3; cy >= -3; cy--) for (int cx = 3; cx >= -3; cx--) m2.GetStreets(new Int2(cx, cy));
            if (h1 != m2.StreetsHash(3)) f.Add("Street networks depend on query order");
            if (h1 == new RegionMap(78).StreetsHash(3)) f.Add("Different seeds gave identical street networks");

            if (GoldenStreetsHash != 0UL && new RegionMap(0xBEEFUL).StreetsHash(3) != GoldenStreetsHash)
                f.Add("Golden streets hash changed: got " + new RegionMap(0xBEEFUL).StreetsHash(3).ToString("X16"));
        }

        static bool IsInterior(RegionMap map, Int2 id, Int2 n)
        {
            return map.RegionAt(new Int2(n.X + 1, n.Y)).Equals(id) && map.RegionAt(new Int2(n.X - 1, n.Y)).Equals(id) &&
                   map.RegionAt(new Int2(n.X, n.Y + 1)).Equals(id) && map.RegionAt(new Int2(n.X, n.Y - 1)).Equals(id);
        }

        public static ulong GoldenStreetsValue() { return new RegionMap(0xBEEFUL).StreetsHash(3); }

        static void Golden(List<string> f)
        {
            if (GoldenStreetHash == 0UL) return;   // not pinned yet
            ulong h = GoldenHash();
            if (h != GoldenStreetHash)
                f.Add("Golden hash changed: got " + h.ToString("X16") + ", pinned " + GoldenStreetHash.ToString("X16"));
        }

        public static ulong GoldenHash()
        {
            var r = Req(11, 22, 24, 3, BuildingFlags.All, 5, BuildingType.Arcade, BuildingType.TowerEntrance);
            return StreetGenerator.Generate(0xC0FFEEUL, r, TestBiomes()[3]).ContentHash();
        }
    }
}
