using System;

namespace EscapeThe90s.WorldGen
{
    public enum BuildingType
    {
        None = 0, Generic, PizzaParlor, WeaponStore, SubwayEntrance, TowerEntrance, Arcade, Bank
    }

    /// <summary>Which special buildings a street is allowed to roll for (your "flags").</summary>
    [Flags]
    public enum BuildingFlags
    {
        None = 0,
        PizzaParlor = 1 << 0, WeaponStore = 1 << 1, SubwayEntrance = 1 << 2,
        TowerEntrance = 1 << 3, Arcade = 1 << 4, Bank = 1 << 5,
        All = ~0
    }

    /// <summary>Reserved for Milestone 3. Streets carry no gate yet.</summary>
    public enum GateKind { None = 0, Boss, Toll, Tower }

    public static class BuildingTypeExt
    {
        public static BuildingFlags ToFlag(this BuildingType t)
        {
            switch (t)
            {
                case BuildingType.PizzaParlor: return BuildingFlags.PizzaParlor;
                case BuildingType.WeaponStore: return BuildingFlags.WeaponStore;
                case BuildingType.SubwayEntrance: return BuildingFlags.SubwayEntrance;
                case BuildingType.TowerEntrance: return BuildingFlags.TowerEntrance;
                case BuildingType.Arcade: return BuildingFlags.Arcade;
                case BuildingType.Bank: return BuildingFlags.Bank;
                default: return BuildingFlags.None;
            }
        }
        public static bool IsSpecial(this BuildingType t) { return t != BuildingType.None && t != BuildingType.Generic; }
    }

    /// <summary>One enemy spawn point inside a chunk. Stable: same seed => same slots, every time.</summary>
    public struct EnemySlot
    {
        public int SlotIndex;      // stable index within the chunk (use for persistence keys)
        public int KindIndex;      // index into the biome's enemy table
        public bool OnBuilding;    // true = stand on the building at AnchorIndex, false = on the ground
        public int AnchorIndex;    // which building anchor (valid when OnBuilding)
        public ushort XFixed;      // horizontal position within the chunk, 0..65535
        public float X { get { return XFixed / 65535f; } }
    }

    public struct ChunkData
    {
        public int Index;
        public BuildingType Building;
        public int Variant;        // which authored design of this building type
        public int Accent;         // palette accent index (colour variety from one design)
        public bool Flipped;       // mirror horizontally (never for buildings with signage)
        public bool IsSafe;        // street-end chunk: no buildings, no enemies
        public EnemySlot[] Enemies;
    }

    public sealed class StreetLayout
    {
        public StableId Id;
        public int Biome;
        public int ChunkCount;
        public ChunkData[] Chunks;
        public int DroppedRequired;     // required buildings that did not fit (street too short)
        public GateKind Gate;           // always None until Milestone 3

        /// <summary>Hash of all content. Used by tests to compare layouts cheaply.</summary>
        public ulong ContentHash()
        {
            ulong h = WorldHash.Combine(Id.Value, Biome);
            h = WorldHash.Combine(h, ChunkCount);
            h = WorldHash.Combine(h, DroppedRequired);
            for (int i = 0; i < Chunks.Length; i++) h = WorldHash.Combine(h, ChunkHash(Chunks[i]));
            return h;
        }

        public static ulong ChunkHash(ChunkData c)
        {
            ulong h = WorldHash.Combine(0, c.Index);
            h = WorldHash.Combine(h, (int)c.Building);
            h = WorldHash.Combine(h, c.Variant);
            h = WorldHash.Combine(h, c.Accent);
            h = WorldHash.Combine(h, c.Flipped ? 1 : 0);
            h = WorldHash.Combine(h, c.IsSafe ? 1 : 0);
            int n = c.Enemies == null ? 0 : c.Enemies.Length;
            h = WorldHash.Combine(h, n);
            for (int i = 0; i < n; i++)
            {
                EnemySlot e = c.Enemies[i];
                h = WorldHash.Combine(h, e.SlotIndex);
                h = WorldHash.Combine(h, e.KindIndex);
                h = WorldHash.Combine(h, e.OnBuilding ? 1 : 0);
                h = WorldHash.Combine(h, e.AnchorIndex);
                h = WorldHash.Combine(h, (int)e.XFixed);
            }
            return h;
        }
    }

    /// <summary>Everything the generator needs to build one street. Built by the region/graph layer later.</summary>
    public struct StreetRequest
    {
        public StableId Id;
        public int ChunkCount;
        public int Biome;                  // index into the biome list
        public BuildingFlags Allowed;      // which special buildings may roll
        public BuildingType[] Required;    // placed first (e.g. the region's arcades); may be null
        public int DifficultyTier;         // from Difficulty.TierAt(distance)
    }
}
