using System;
using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    public enum SpawnLocationKind { Enemy = 0, PowerUp, BuildingAnchor }

    /// <summary>A normalized spawn point emitted with a chunk for downstream content systems.</summary>
    public readonly struct SpawnLocation
    {
        public readonly SpawnLocationKind Kind;
        public readonly float NormalizedX;
        public readonly int SlotIndex;
        public readonly int ContentKindIndex;
        public readonly int AnchorIndex;

        public SpawnLocation(SpawnLocationKind kind, float normalizedX, int slotIndex, int contentKindIndex, int anchorIndex)
        {
            Kind = kind;
            NormalizedX = Clamp01(normalizedX);
            SlotIndex = slotIndex;
            ContentKindIndex = contentKindIndex;
            AnchorIndex = anchorIndex;
        }

        static float Clamp01(float value)
        {
            if (float.IsNaN(value) || value <= 0f) return 0f;
            if (value >= 1f) return 1f;
            return value;
        }
    }

    /// <summary>Seeded terrain instruction in chunk-local cell coordinates. Height values are surface offsets.</summary>
    public readonly struct TerrainStamp
    {
        public readonly TerrainStampKind Kind;
        public readonly int StartCell;
        public readonly int WidthCells;
        public readonly int StartHeight;
        public readonly int EndHeight;

        public TerrainStamp(TerrainStampKind kind, int startCell, int widthCells, int startHeight, int endHeight)
        {
            Kind = kind;
            StartCell = startCell;
            WidthCells = widthCells;
            StartHeight = startHeight;
            EndHeight = endHeight;
        }

        public static TerrainStamp Flat { get { return new TerrainStamp(TerrainStampKind.Flat, 0, 0, 0, 0); } }
    }

    /// <summary>Generated, renderer-independent chunk data plus its gameplay and terrain metadata.</summary>
    public readonly struct ChunkSpec
    {
        static readonly IReadOnlyList<SpawnLocation> EmptyLocations = Array.AsReadOnly(new SpawnLocation[0]);
        readonly IReadOnlyList<SpawnLocation> _spawnLocations;

        public readonly ChunkData Content;
        public readonly ChunkKind Kind;
        public readonly TerrainStamp Stamp;
        public readonly StreetKind StreetKind;
        public readonly int Biome;
        public readonly float Difficulty;

        public IReadOnlyList<SpawnLocation> SpawnLocations
        { get { return _spawnLocations ?? EmptyLocations; } }

        public ChunkSpec(ChunkData content, ChunkKind kind, TerrainStamp stamp, StreetKind streetKind,
            int biome, float difficulty, SpawnLocation[] spawnLocations)
        {
            Content = content;
            Kind = kind;
            Stamp = stamp;
            StreetKind = streetKind;
            Biome = biome;
            Difficulty = DifficultyProgression.ClampDifficulty(difficulty);
            SpawnLocation[] copy = spawnLocations == null ? new SpawnLocation[0] : (SpawnLocation[])spawnLocations.Clone();
            _spawnLocations = Array.AsReadOnly(copy);
        }

        public ulong MetadataHash()
        {
            ulong h = WorldHash.Combine(0, (int)Kind);
            h = WorldHash.Combine(h, (int)Stamp.Kind);
            h = WorldHash.Combine(h, Stamp.StartCell);
            h = WorldHash.Combine(h, Stamp.WidthCells);
            h = WorldHash.Combine(h, Stamp.StartHeight);
            h = WorldHash.Combine(h, Stamp.EndHeight);
            h = WorldHash.Combine(h, (int)StreetKind);
            h = WorldHash.Combine(h, Biome);
            h = WorldHash.Combine(h, (int)(Difficulty * 1000f));
            foreach (SpawnLocation location in SpawnLocations)
            {
                h = WorldHash.Combine(h, (int)location.Kind);
                h = WorldHash.Combine(h, (int)(location.NormalizedX * 65535f));
                h = WorldHash.Combine(h, location.SlotIndex);
                h = WorldHash.Combine(h, location.ContentKindIndex);
                h = WorldHash.Combine(h, location.AnchorIndex);
            }
            return h;
        }
    }

    /// <summary>Strategy boundary for generating a street chunk from its stable inputs.</summary>
    public interface IChunkGenerator
    {
        ChunkSpec Generate(ulong seed, StableId streetId, int index, int chunkCount, BuildingType building,
            BiomeRules rules, int biome, StreetKind streetKind, float difficulty);
    }

    /// <summary>Default deterministic chunk strategy. Terrain stamps use their own RNG salt.</summary>
    public sealed class DeterministicChunkGenerator : IChunkGenerator
    {
        public ChunkSpec Generate(ulong seed, StableId streetId, int index, int chunkCount, BuildingType building,
            BiomeRules rules, int biome, StreetKind streetKind, float difficulty)
        {
            int tier = DifficultyProgression.TierAtDifficulty(difficulty);
            ChunkData content = StreetGenerator.GenerateChunk(seed, streetId, index, chunkCount, building, rules, tier);
            TerrainStamp stamp = TerrainStampLibrary.Create(seed, streetId, index, streetKind, difficulty, content.IsSafe, building);
            var locations = new SpawnLocation[content.Enemies == null ? 0 : content.Enemies.Length];
            for (int i = 0; i < locations.Length; i++)
            {
                EnemySlot enemy = content.Enemies[i];
                locations[i] = new SpawnLocation(SpawnLocationKind.Enemy, enemy.X, enemy.SlotIndex,
                    enemy.KindIndex, enemy.AnchorIndex);
            }
            return new ChunkSpec(content, ChunkKindFor(stamp.Kind), stamp, streetKind, biome, difficulty, locations);
        }

        static ChunkKind ChunkKindFor(TerrainStampKind stamp)
        {
            if (stamp == TerrainStampKind.Gap) return ChunkKind.Gap;
            if (stamp == TerrainStampKind.StairsUp || stamp == TerrainStampKind.StairsDown) return ChunkKind.Ramp;
            if (stamp == TerrainStampKind.Plaza) return ChunkKind.Plaza;
            return ChunkKind.Ground;
        }
    }

    /// <summary>Selects bounded deterministic stamps; the Unity renderer consumes their geometry in M7.</summary>
    public static class TerrainStampLibrary
    {
        public static TerrainStamp Create(ulong seed, StableId streetId, int index, StreetKind streetKind,
            float difficulty, bool safe, BuildingType building)
        {
            if (safe || building != BuildingType.None) return TerrainStamp.Flat;
            var rng = DeterministicRng.Create(seed, streetId, index, Salt.Stamps);
            int challenge = DifficultyProgression.TierAtDifficulty(difficulty);
            int gapChance = GapChance(streetKind, challenge);
            int rampChance = RampChance(streetKind);
            int curbChance = CurbChance(streetKind);
            int roll = rng.Next(100);
            if (roll < gapChance) return MakeGap(ref rng);
            if (roll < gapChance + rampChance) return MakeStairs(ref rng, roll < gapChance + rampChance / 2);
            if (roll < gapChance + rampChance + curbChance) return MakeCurb(ref rng);
            if (roll < gapChance + rampChance + curbChance + 8) return MakeObstacle(ref rng);
            return TerrainStamp.Flat;
        }

        static int GapChance(StreetKind kind, int challenge)
        {
            int baseChance = kind == StreetKind.Avenue ? 10 : kind == StreetKind.Street ? 6 : 3;
            return Math.Min(15, baseChance + Math.Max(0, challenge));
        }

        static int RampChance(StreetKind kind)
        { return kind == StreetKind.Avenue ? 28 : kind == StreetKind.Street ? 20 : 12; }

        static int CurbChance(StreetKind kind)
        { return kind == StreetKind.Alley ? 12 : 8; }

        static TerrainStamp MakeGap(ref DeterministicRng rng)
        {
            int width = rng.Range(2, WorldConstants.MaxGapWidthCells + 1);
            int start = rng.Range(5, WorldConstants.ChunkWidthCells - width - 5);
            return new TerrainStamp(TerrainStampKind.Gap, start, width, 0, 0);
        }

        static TerrainStamp MakeStairs(ref DeterministicRng rng, bool up)
        {
            int width = rng.Range(6, 11);
            int start = rng.Range(3, WorldConstants.ChunkWidthCells - width - 3);
            int rise = up ? WorldConstants.MaxBumpHeight : -WorldConstants.MaxBumpHeight;
            return new TerrainStamp(up ? TerrainStampKind.StairsUp : TerrainStampKind.StairsDown,
                start, width, 0, rise);
        }

        static TerrainStamp MakeCurb(ref DeterministicRng rng)
        {
            int width = rng.Range(3, 7);
            int start = rng.Range(3, WorldConstants.ChunkWidthCells - width - 3);
            return new TerrainStamp(TerrainStampKind.Curb, start, width, 0, 1);
        }

        static TerrainStamp MakeObstacle(ref DeterministicRng rng)
        {
            int start = rng.Range(4, WorldConstants.ChunkWidthCells - 4);
            return new TerrainStamp(TerrainStampKind.Obstacle, start, 1, 0, 0);
        }
    }
}
