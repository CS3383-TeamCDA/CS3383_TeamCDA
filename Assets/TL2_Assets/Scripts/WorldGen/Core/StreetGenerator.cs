using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Builds a StreetLayout from (seed, request, biome rules). Pure function: no globals, no Unity.
    /// Street-level step decides WHICH building goes in WHICH chunk (needs coordination, so it is
    /// computed per street and cached). Everything else about a chunk comes from its own RNG
    /// streams, so a chunk can be rebuilt alone from (seed, street id, index, building).
    /// </summary>
    public static class StreetGenerator
    {
        public static StreetLayout Generate(ulong seed, StreetRequest req, BiomeRules rules)
        {
            int n = req.ChunkCount < WorldConstants.MinStreetChunks ? WorldConstants.MinStreetChunks : req.ChunkCount;
            var layout = new StreetLayout { Id = req.Id, Biome = req.Biome, ChunkCount = n, Gate = GateKind.None };

            int dropped;
            BuildingType[] placement = PlaceBuildings(seed, req, rules, n, out dropped);
            layout.DroppedRequired = dropped;

            layout.Chunks = new ChunkData[n];
            for (int i = 0; i < n; i++)
                layout.Chunks[i] = GenerateChunk(seed, req.Id, i, n, placement[i], rules, req.DifficultyTier);
            return layout;
        }

        static bool IsSafeChunk(int index, int count)
        {
            return index < WorldConstants.EndSafeChunks || index >= count - WorldConstants.EndSafeChunks;
        }

        static BuildingType[] PlaceBuildings(ulong seed, StreetRequest req, BiomeRules rules, int n, out int dropped)
        {
            var slots = new BuildingType[n];
            var free = new List<int>();
            for (int i = 0; i < n; i++) if (!IsSafeChunk(i, n)) free.Add(i);

            var rng = DeterministicRng.Create(seed, req.Id, Salt.Placement);
            var counts = new Dictionary<BuildingType, int>();
            dropped = 0;

            // 1) Required buildings first (e.g. the region's arcades). Ignore density and caps.
            if (req.Required != null)
            {
                for (int k = 0; k < req.Required.Length; k++)
                {
                    if (free.Count == 0) { dropped++; continue; }
                    int pick = rng.Next(free.Count);
                    slots[free[pick]] = req.Required[k];
                    free.RemoveAt(pick);
                    Bump(counts, req.Required[k]);
                }
            }

            // 2) Optional specials: rolled once per street, only if flagged. Rule order is data order.
            for (int r = 0; r < rules.Buildings.Length; r++)
            {
                BuildingRule rule = rules.Buildings[r];
                if (!rule.Type.IsSpecial() || rule.PerStreetPercent <= 0) continue;
                if ((req.Allowed & rule.Type.ToFlag()) == 0) continue;
                int have = Get(counts, rule.Type);
                while (have < rule.MaxPerStreet && free.Count > 0 && rng.ChancePercent(rule.PerStreetPercent))
                {
                    int pick = rng.Next(free.Count);
                    slots[free[pick]] = rule.Type;
                    free.RemoveAt(pick);
                    have++;
                    Bump(counts, rule.Type);
                }
            }

            // 3) Filler: each remaining free chunk may get one building (max 1 per chunk).
            var weights = new int[rules.Buildings.Length];
            for (int f = 0; f < free.Count; f++)
            {
                if (!rng.ChancePercent(rules.BuildingDensityPercent)) continue;
                for (int r = 0; r < weights.Length; r++)
                {
                    BuildingRule rule = rules.Buildings[r];
                    bool allowed = rule.Type == BuildingType.Generic || (req.Allowed & rule.Type.ToFlag()) != 0;
                    bool underCap = Get(counts, rule.Type) < rule.MaxPerStreet;
                    weights[r] = (rule.FillWeight > 0 && allowed && underCap) ? rule.FillWeight : 0;
                }
                int idx = rng.PickWeighted(weights);
                if (idx < 0) continue;
                slots[free[f]] = rules.Buildings[idx].Type;
                Bump(counts, rules.Buildings[idx].Type);
            }
            return slots;
        }

        static int Get(Dictionary<BuildingType, int> d, BuildingType t) { int v; return d.TryGetValue(t, out v) ? v : 0; }
        static void Bump(Dictionary<BuildingType, int> d, BuildingType t) { d[t] = Get(d, t) + 1; }

        /// <summary>
        /// One chunk, built only from (seed, street id, index, building). Independent of every other chunk.
        /// Look and enemies use separate RNG streams, so tuning enemy rules never reshuffles visuals.
        /// </summary>
        public static ChunkData GenerateChunk(ulong seed, StableId streetId, int index, int chunkCount,
                                              BuildingType building, BiomeRules rules, int difficultyTier)
        {
            bool safe = IsSafeChunk(index, chunkCount);
            var chunk = new ChunkData { Index = index, Building = building, IsSafe = safe, Enemies = new EnemySlot[0] };
            BuildingRule rule = building == BuildingType.None ? null : rules.FindRule(building);

            var look = DeterministicRng.Create(seed, streetId, index, Salt.ChunkLook);
            int variantCount = rule != null && rule.VariantCount > 1 ? rule.VariantCount : 1;
            chunk.Variant = look.Next(variantCount);
            chunk.Accent = look.Next(rules.AccentCount < 1 ? 1 : rules.AccentCount);
            bool flipRoll = look.ChancePercent(50);            // always consumed, so streams stay aligned
            chunk.Flipped = flipRoll && (rule == null || rule.AllowFlip);

            if (safe) return chunk;

            var en = DeterministicRng.Create(seed, streetId, index, Salt.Enemies);
            int count = en.Range(rules.EnemiesMinPerChunk, rules.EnemiesMaxPerChunk + 1);
            if (rules.TiersPerExtraEnemy > 0) count += difficultyTier / rules.TiersPerExtraEnemy;
            if (count > rules.EnemyHardCap) count = rules.EnemyHardCap;
            if (count < 0) count = 0;

            int anchors = rule != null ? rule.EnemyAnchors : 0;
            var slots = new EnemySlot[count];
            for (int s = 0; s < count; s++)
            {
                bool onBuilding = s < anchors;                  // fill building anchors first, rest on the ground
                int kind = en.PickWeighted(rules.EnemyKindWeights);
                slots[s] = new EnemySlot
                {
                    SlotIndex = s,
                    KindIndex = kind < 0 ? 0 : kind,
                    OnBuilding = onBuilding,
                    AnchorIndex = onBuilding ? s : -1,
                    XFixed = (ushort)(en.NextUInt() >> 16),
                };
            }
            chunk.Enemies = slots;
            return chunk;
        }
    }
}
