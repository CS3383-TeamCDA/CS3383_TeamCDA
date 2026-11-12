using System;
using System.Collections.Generic;

namespace EscapeThe90s.Contracts
{
    /// <summary>Immutable event payload describing a chunk ready for dependent content systems.</summary>
    public sealed class ChunkSpawnedInfo
    {
        readonly IReadOnlyList<SpawnLocation> _spawnLocations;

        public ChunkId Id { get; private set; }
        public ChunkBounds Bounds { get; private set; }
        public float Difficulty { get; private set; }
        public int Tier { get; private set; }
        public int Biome { get; private set; }
        public ChunkKind Kind { get; private set; }
        public IReadOnlyList<SpawnLocation> SpawnLocations { get { return _spawnLocations; } }

        public ChunkSpawnedInfo(ChunkId id, ChunkBounds bounds, float difficulty, int tier,
            int biome, ChunkKind kind, IReadOnlyList<SpawnLocation> spawnLocations)
        {
            Id = id;
            Bounds = bounds;
            Difficulty = ClampDifficulty(difficulty);
            Tier = Math.Max(0, tier);
            Biome = biome;
            Kind = kind;
            int count = spawnLocations == null ? 0 : spawnLocations.Count;
            var copy = new SpawnLocation[count];
            for (int i = 0; i < count; i++) copy[i] = spawnLocations[i];
            _spawnLocations = Array.AsReadOnly(copy);
        }

        static float ClampDifficulty(float difficulty)
        {
            if (float.IsNaN(difficulty) || difficulty <= 0f) return 0f;
            if (float.IsInfinity(difficulty) || difficulty >= 10f) return 10f;
            return difficulty;
        }
    }
}
