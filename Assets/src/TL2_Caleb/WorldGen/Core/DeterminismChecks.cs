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
