using System;
using System.Collections.Generic;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Immutable generation snapshot for one 16 by 16 coordinate chunk.</summary>
    public sealed class WorldChunkData
    {
        readonly TileId[] _tiles;
        readonly IReadOnlyList<SpawnLocation> _spawnLocations;

        public WorldChunkCoord Coordinate { get; private set; }
        public WorldChunkKind Kind { get; private set; }
        public int Biome { get; private set; }
        public int WorldFloorCellY { get; private set; }
        public float Difficulty { get; private set; }
        public ulong ContentHash { get; private set; }
        public IReadOnlyList<SpawnLocation> SpawnLocations { get { return _spawnLocations; } }

        public WorldChunkData(WorldChunkCoord coordinate, WorldChunkKind kind, int biome, int worldFloorCellY, float difficulty,
            ulong contentHash, TileId[] tiles, IReadOnlyList<SpawnLocation> spawnLocations)
        {
            if (tiles == null || tiles.Length != WorldChunkCoord.Size * WorldChunkCoord.Size)
                throw new ArgumentException("A world chunk requires exactly 16 by 16 tile values.", "tiles");
            Coordinate = coordinate;
            Kind = kind;
            Biome = Math.Max(0, biome);
            WorldFloorCellY = worldFloorCellY;
            Difficulty = DifficultyProgression.ClampDifficulty(difficulty);
            ContentHash = contentHash;
            _tiles = (TileId[])tiles.Clone();
            int count = spawnLocations == null ? 0 : spawnLocations.Count;
            var copy = new SpawnLocation[count];
            for (int i = 0; i < count; i++) copy[i] = spawnLocations[i];
            _spawnLocations = Array.AsReadOnly(copy);
        }

        public TileId GetTile(int localX, int localY)
        {
            if (localX < 0 || localX >= WorldChunkCoord.Size) throw new ArgumentOutOfRangeException("localX");
            if (localY < 0 || localY >= WorldChunkCoord.Size) throw new ArgumentOutOfRangeException("localY");
            return _tiles[localY * WorldChunkCoord.Size + localX];
        }
    }
}
