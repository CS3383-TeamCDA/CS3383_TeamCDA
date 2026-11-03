using System;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Per-building tuning inside one biome. Integers only, so results are platform-stable.</summary>
    [Serializable]
    public class BuildingRule
    {
        public BuildingType Type = BuildingType.Generic;
        public int FillWeight = 0;        // >0: can appear as ordinary filler
        public int PerStreetPercent = 0;  // >0: rolled once per street (special buildings; needs the flag)
        public int MaxPerStreet = 99;     // cap for optional placement (Required ignores it)
        public int VariantCount = 1;      // authored designs of this building in this biome
        public int EnemyAnchors = 0;      // spots on/around the building enemies can attach to
        public bool AllowFlip = true;     // false for buildings with readable signage
    }

    /// <summary>Plain data, no Unity types: serialisable in a ScriptableObject and testable outside Unity.</summary>
    [Serializable]
    public class BiomeRules
    {
        public string Name = "Biome";
        public int BuildingDensityPercent = 65;   // chance a free chunk gets a filler building
        public int AccentCount = 3;
        public int EnemiesMinPerChunk = 1;
        public int EnemiesMaxPerChunk = 3;        // inclusive
        public int TiersPerExtraEnemy = 2;        // +1 enemy per this many difficulty tiers
        public int EnemyHardCap = 8;
        public int[] EnemyKindWeights = { 1 };
        public BuildingRule[] Buildings = new BuildingRule[0];

        public BuildingRule FindRule(BuildingType t)
        {
            for (int i = 0; i < Buildings.Length; i++) if (Buildings[i].Type == t) return Buildings[i];
            return null;
        }

        /// <summary>Sensible placeholder values matching your notes (pizza 20% per street, etc.).</summary>
        public static BiomeRules CreateDefault(string name)
        {
            var r = new BiomeRules { Name = name };
            r.Buildings = new[]
            {
                new BuildingRule { Type = BuildingType.Generic,        FillWeight = 10, EnemyAnchors = 2 },
                new BuildingRule { Type = BuildingType.PizzaParlor,    PerStreetPercent = 20, MaxPerStreet = 1, AllowFlip = false },
                new BuildingRule { Type = BuildingType.WeaponStore,    PerStreetPercent = 10, MaxPerStreet = 1, AllowFlip = false },
                new BuildingRule { Type = BuildingType.Bank,           PerStreetPercent = 5,  MaxPerStreet = 1, AllowFlip = false },
                new BuildingRule { Type = BuildingType.SubwayEntrance, MaxPerStreet = 1, AllowFlip = false },
                new BuildingRule { Type = BuildingType.TowerEntrance,  MaxPerStreet = 2, AllowFlip = false },
                new BuildingRule { Type = BuildingType.Arcade,         MaxPerStreet = 1, AllowFlip = false, EnemyAnchors = 3 },
            };
            return r;
        }
    }
}
