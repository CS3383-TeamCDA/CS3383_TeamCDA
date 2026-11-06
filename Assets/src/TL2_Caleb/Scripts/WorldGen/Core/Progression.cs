using System;
using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Continuous difficulty curve with a rebase point for run initialization and tests.</summary>
    public sealed class DifficultyProgression
    {
        public const float DefaultMaxDifficulty = 10f;
        readonly float _distancePerDifficulty;
        readonly float _maxDifficulty;
        float _anchorDistance;
        float _anchorDifficulty;

        public DifficultyProgression(float distancePerDifficulty = WorldConstants.DistancePerTier,
            float maxDifficulty = DefaultMaxDifficulty)
        {
            _distancePerDifficulty = float.IsNaN(distancePerDifficulty) || float.IsInfinity(distancePerDifficulty) || distancePerDifficulty <= 0f
                ? WorldConstants.DistancePerTier : distancePerDifficulty;
            _maxDifficulty = ClampDifficulty(maxDifficulty, DefaultMaxDifficulty);
        }

        public float MaxDifficulty { get { return _maxDifficulty; } }

        public float DifficultyAt(float distance)
        {
            float safeDistance = SanitizeDistance(distance);
            return ClampDifficulty(_anchorDifficulty + (safeDistance - _anchorDistance) / _distancePerDifficulty, _maxDifficulty);
        }

        public void Rebase(float distance, float difficulty)
        {
            _anchorDistance = SanitizeDistance(distance);
            _anchorDifficulty = ClampDifficulty(difficulty, _maxDifficulty);
        }

        public static int TierAtDifficulty(float difficulty)
        {
            float safe = ClampDifficulty(difficulty, DefaultMaxDifficulty);
            return (int)Math.Floor(safe);
        }

        static float SanitizeDistance(float distance)
        {
            if (float.IsNaN(distance) || distance <= 0f) return 0f;
            return float.IsPositiveInfinity(distance) ? float.MaxValue : distance;
        }

        public static float ClampDifficulty(float difficulty, float maximum = DefaultMaxDifficulty)
        {
            if (float.IsNaN(difficulty) || difficulty <= 0f) return 0f;
            if (float.IsPositiveInfinity(difficulty) || difficulty >= maximum) return maximum;
            return difficulty;
        }
    }

    /// <summary>Stable identity for one streamed street chunk.</summary>
    public readonly struct ChunkId : IEquatable<ChunkId>
    {
        public readonly StableId StreetId;
        public readonly int Index;

        public ChunkId(StableId streetId, int index) { StreetId = streetId; Index = index; }
        public bool Equals(ChunkId other) { return StreetId.Equals(other.StreetId) && Index == other.Index; }
        public override bool Equals(object obj) { return obj is ChunkId other && Equals(other); }
        public override int GetHashCode() { unchecked { return StreetId.GetHashCode() * 397 ^ Index; } }
    }

    /// <summary>Engine-free world-space bounds included in chunk lifecycle events.</summary>
    public readonly struct ChunkBounds
    {
        public readonly float MinX, MinY, MaxX, MaxY;
        public ChunkBounds(float minX, float minY, float maxX, float maxY)
        { MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY; }
    }

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
        public IReadOnlyList<SpawnLocation> SpawnLocations
        { get { return _spawnLocations; } }

        public ChunkSpawnedInfo(ChunkId id, ChunkBounds bounds, float difficulty, int tier, int biome,
            ChunkKind kind, IReadOnlyList<SpawnLocation> spawnLocations)
        {
            Id = id;
            Bounds = bounds;
            Difficulty = DifficultyProgression.ClampDifficulty(difficulty);
            Tier = Math.Max(0, tier);
            Biome = biome;
            Kind = kind;
            int count = spawnLocations == null ? 0 : spawnLocations.Count;
            var copy = new SpawnLocation[count];
            for (int i = 0; i < count; i++) copy[i] = spawnLocations[i];
            _spawnLocations = Array.AsReadOnly(copy);
        }
    }

    /// <summary>Progress and chunk-lifecycle contract exposed by the level generator.</summary>
    public interface ILevelProgress
    {
        float GetCurrentDistance();
        float GetCurrentDifficulty();
        void SetDifficulty(float difficulty);
        event Action<ChunkSpawnedInfo> OnChunkSpawned;
        event Action<ChunkId> OnChunkRecycled;
    }

    /// <summary>Tracks furthest progress and publishes each chunk's lifecycle once per active load.</summary>
    public sealed class LevelProgress : ILevelProgress
    {
        readonly DifficultyProgression _difficulty;
        readonly HashSet<ChunkId> _activeChunks = new HashSet<ChunkId>();
        float _currentDistance;

        public event Action<ChunkSpawnedInfo> OnChunkSpawned;
        public event Action<ChunkId> OnChunkRecycled;

        public LevelProgress(DifficultyProgression difficulty = null)
        { _difficulty = difficulty ?? new DifficultyProgression(); }

        public float GetCurrentDistance() { return _currentDistance; }
        public float GetCurrentDifficulty() { return _difficulty.DifficultyAt(_currentDistance); }
        public float GetDifficultyAt(float distance) { return _difficulty.DifficultyAt(distance); }
        public int GetTierAt(float distance) { return DifficultyProgression.TierAtDifficulty(GetDifficultyAt(distance)); }

        public void AdvanceDistance(float distance)
        {
            if (float.IsNaN(distance) || distance <= _currentDistance) return;
            _currentDistance = float.IsPositiveInfinity(distance) ? float.MaxValue : distance;
        }

        public void SetDifficulty(float difficulty)
        { _difficulty.Rebase(_currentDistance, difficulty); }

        public bool NotifyChunkSpawned(ChunkSpawnedInfo info)
        {
            if (info == null || !_activeChunks.Add(info.Id)) return false;
            Action<ChunkSpawnedInfo> handler = OnChunkSpawned;
            if (handler != null) handler(info);
            return true;
        }

        public bool NotifyChunkRecycled(ChunkId id)
        {
            if (!_activeChunks.Remove(id)) return false;
            Action<ChunkId> handler = OnChunkRecycled;
            if (handler != null) handler(id);
            return true;
        }
    }
}
