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
            : this(coordinate, kind, biome, worldFloorCellY, difficulty, contentHash, tiles, spawnLocations, true) { }

        /// <summary>Transfers fresh generator-owned arrays into an immutable snapshot without redundant copies.</summary>
        internal static WorldChunkData FromOwnedArrays(WorldChunkCoord coordinate, WorldChunkKind kind, int biome,
            int worldFloorCellY, float difficulty, ulong contentHash, TileId[] tiles, SpawnLocation[] spawnLocations)
        { return new WorldChunkData(coordinate, kind, biome, worldFloorCellY, difficulty, contentHash, tiles, spawnLocations, false); }

        WorldChunkData(WorldChunkCoord coordinate, WorldChunkKind kind, int biome, int worldFloorCellY, float difficulty,
            ulong contentHash, TileId[] tiles, IReadOnlyList<SpawnLocation> spawnLocations, bool copyInputs)
        {
            if (tiles == null || tiles.Length != WorldChunkCoord.Size * WorldChunkCoord.Size)
                throw new ArgumentException("A world chunk requires exactly 16 by 16 tile values.", "tiles");
            Coordinate = coordinate;
            Kind = kind;
            Biome = Math.Max(0, biome);
            WorldFloorCellY = worldFloorCellY;
            Difficulty = DifficultyProgression.ClampDifficulty(difficulty);
            ContentHash = contentHash;
            _tiles = copyInputs ? (TileId[])tiles.Clone() : tiles;
            int count = spawnLocations == null ? 0 : spawnLocations.Count;
            SpawnLocation[] copy = count == 0 ? Array.Empty<SpawnLocation>()
                : copyInputs ? new SpawnLocation[count] : (SpawnLocation[])spawnLocations;
            if (copyInputs) for (int i = 0; i < count; i++) copy[i] = spawnLocations[i];
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
