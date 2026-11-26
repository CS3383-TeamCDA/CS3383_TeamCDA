using System;
using System.Collections.Generic;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Immutable generation snapshot for one 16 by 16 coordinate chunk.</summary>
    public sealed class WorldChunkData
    {
        readonly TileId[] _tiles;
        readonly byte[] _groundEnds;
        readonly IReadOnlyList<SpawnLocation> _spawnLocations = Array.Empty<SpawnLocation>();

        public WorldChunkCoord Coordinate { get; private set; }
        public WorldChunkKind Kind { get; private set; }
        public ChunkKind TerrainKind { get; private set; }
        public int Biome { get; private set; }
        public int WorldFloorCellY { get; private set; }
        public float Difficulty { get; private set; }
        public ulong ContentHash { get; private set; }
        public IReadOnlyList<SpawnLocation> SpawnLocations { get { return _spawnLocations; } }

        public WorldChunkData(WorldChunkCoord coordinate, WorldChunkKind kind, int biome, int worldFloorCellY, float difficulty,
            ulong contentHash, TileId[] tiles, IReadOnlyList<SpawnLocation> spawnLocations)
            : this(coordinate, kind, biome, worldFloorCellY, difficulty, contentHash, ChunkKind.Ground)
        {
            if (tiles == null || tiles.Length != WorldChunkCoord.Size * WorldChunkCoord.Size)
                throw new ArgumentException("A world chunk requires exactly 16 by 16 tile values.", nameof(tiles));
            _tiles = (TileId[])tiles.Clone();
            int count = spawnLocations == null ? 0 : spawnLocations.Count;
            if (count == 0) return;
            var copy = new SpawnLocation[count];
            for (int i = 0; i < count; i++) copy[i] = spawnLocations[i];
            _spawnLocations = Array.AsReadOnly(copy);
        }

        // The side-scroller has one ground interval per column above the world floor.
        // Both arrays belong exclusively to this snapshot after generation.
        internal WorldChunkData(WorldChunkCoord coordinate, WorldChunkKind kind, int biome,
            int worldFloorCellY, float difficulty, ulong contentHash, byte[] groundEnds,
            SpawnLocation[] spawnLocations, ChunkKind terrainKind)
            : this(coordinate, kind, biome, worldFloorCellY, difficulty, contentHash, terrainKind)
        {
            _groundEnds = groundEnds;
            if (spawnLocations.Length > 0) _spawnLocations = Array.AsReadOnly(spawnLocations);
        }

        WorldChunkData(WorldChunkCoord coordinate, WorldChunkKind kind, int biome, int worldFloorCellY,
            float difficulty, ulong contentHash, ChunkKind terrainKind)
        {
            Coordinate = coordinate;
            Kind = kind;
            TerrainKind = terrainKind;
            Biome = Math.Max(0, biome);
            WorldFloorCellY = worldFloorCellY;
            Difficulty = DifficultyProgression.ClampDifficulty(difficulty);
            ContentHash = contentHash;
        }

        public TileId GetTile(int localX, int localY)
        {
            if (localX < 0 || localX >= WorldChunkCoord.Size) throw new ArgumentOutOfRangeException("localX");
            if (localY < 0 || localY >= WorldChunkCoord.Size) throw new ArgumentOutOfRangeException("localY");
            if (_tiles != null) return _tiles[localY * WorldChunkCoord.Size + localX];
            if (Coordinate.MinCellY + localY <= WorldFloorCellY) return TileId.Void;
            return localY < _groundEnds[localX] ? TileId.Ground : TileId.Sky;
        }
    }
}
