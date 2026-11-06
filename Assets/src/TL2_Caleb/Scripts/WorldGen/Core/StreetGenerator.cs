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
        static readonly IChunkGenerator DefaultChunkGenerator = new DeterministicChunkGenerator();

        public static StreetLayout Generate(ulong seed, StreetRequest req, BiomeRules rules)
        { return Generate(seed, req, rules, DefaultChunkGenerator); }

        public static StreetLayout Generate(ulong seed, StreetRequest req, BiomeRules rules, IChunkGenerator chunkGenerator)
        {
            if (chunkGenerator == null) throw new System.ArgumentNullException("chunkGenerator");
            int n = req.ChunkCount < WorldConstants.MinStreetChunks ? WorldConstants.MinStreetChunks : req.ChunkCount;
            var layout = new StreetLayout { Id = req.Id, Biome = req.Biome, Kind = req.Kind,
                ChunkCount = n, Gate = GateKind.None };

            int dropped;
            BuildingType[] placement = PlaceBuildings(seed, req, rules, n, out dropped);
            layout.DroppedRequired = dropped;

            layout.Chunks = new ChunkData[n];
            layout.ChunkSpecs = new ChunkSpec[n];
            float difficulty = req.Difficulty;
            if (float.IsNaN(difficulty) || (difficulty <= 0f && req.DifficultyTier > 0)) difficulty = req.DifficultyTier;
            for (int i = 0; i < n; i++)
            {
                ChunkSpec spec = chunkGenerator.Generate(seed, req.Id, i, n, placement[i], rules,
                    req.Biome, req.Kind, difficulty);
                layout.ChunkSpecs[i] = spec;
                layout.Chunks[i] = spec.Content;
            }
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
            PlaceRequiredBuildings(req.Required, slots, free, counts, ref rng, out dropped);
            PlaceOptionalSpecials(req.Allowed, rules.Buildings, slots, free, counts, ref rng);
            PlaceFillerBuildings(req.Allowed, rules, slots, free, counts, ref rng);
            return slots;
        }

        static void PlaceRequiredBuildings(BuildingType[] required, BuildingType[] slots, List<int> free,
            Dictionary<BuildingType, int> counts, ref DeterministicRng rng, out int dropped)
        {
            dropped = 0;
            if (required == null) return;
            for (int i = 0; i < required.Length; i++)
            {
                if (free.Count == 0) { dropped++; continue; }
                int pick = rng.Next(free.Count);
                slots[free[pick]] = required[i];
                free.RemoveAt(pick);
                Bump(counts, required[i]);
            }
        }

        static void PlaceOptionalSpecials(BuildingFlags allowed, BuildingRule[] rules, BuildingType[] slots,
            List<int> free, Dictionary<BuildingType, int> counts, ref DeterministicRng rng)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                BuildingRule rule = rules[i];
                if (!rule.Type.IsSpecial() || rule.PerStreetPercent <= 0) continue;
                if ((allowed & rule.Type.ToFlag()) == 0) continue;
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
        }

        static void PlaceFillerBuildings(BuildingFlags allowed, BiomeRules rules, BuildingType[] slots,
            List<int> free, Dictionary<BuildingType, int> counts, ref DeterministicRng rng)
        {
            var weights = new int[rules.Buildings.Length];
            for (int f = 0; f < free.Count; f++)
            {
                if (!rng.ChancePercent(rules.BuildingDensityPercent)) continue;
                for (int r = 0; r < weights.Length; r++)
                    weights[r] = FillerWeight(rules.Buildings[r], allowed, counts);
                int index = rng.PickWeighted(weights);
                if (index < 0) continue;
                BuildingType building = rules.Buildings[index].Type;
                slots[free[f]] = building;
                Bump(counts, building);
            }
        }

        static int FillerWeight(BuildingRule rule, BuildingFlags allowed, Dictionary<BuildingType, int> counts)
        {
            bool canPlace = rule.Type == BuildingType.Generic || (allowed & rule.Type.ToFlag()) != 0;
            bool underCap = Get(counts, rule.Type) < rule.MaxPerStreet;
            return rule.FillWeight > 0 && canPlace && underCap ? rule.FillWeight : 0;
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
