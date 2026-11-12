using System.Collections.Generic;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    /// <summary>World profile for an endless horizontal run with vertical ground and sky fill.</summary>
    public sealed class SideScrollerWorldProfile : IWorldProfile
    {
        readonly ulong _seed;
        readonly CoordinateWorldGenerator _generator;
        readonly CoordinateTerrainStampField _terrainStamps;
        readonly WorldBiomeField _biomeField;
        readonly int _surfaceBaseline;
        readonly int _worldFloor;
        readonly int _distancePerDifficulty;

        public SideScrollerWorldProfile(ulong seed, int surfaceBaseline = WorldConstants.WorldSurfaceBaseline,
            int surfaceVariation = WorldConstants.WorldSurfaceVariationCells,
            int anchorSpan = WorldConstants.WorldSurfaceAnchorSpanCells,
            int worldFloor = WorldConstants.WorldFloorCellY,
            int distancePerDifficulty = WorldConstants.WorldDistancePerDifficulty,
            int biomeCount = WorldConstants.BiomeCount,
            int biomeRegionSpanChunks = WorldConstants.WorldBiomeRegionSpanChunks,
            int cityBlockLengthCells = WorldConstants.WorldCityBlockLengthChunks * WorldChunkCoord.Size,
            int cityBlockFlatHalfWidthCells = WorldConstants.WorldCityBlockFlatHalfWidthCells)
        {
            _seed = seed;
            _surfaceBaseline = surfaceBaseline;
            _worldFloor = worldFloor;
            _distancePerDifficulty = distancePerDifficulty <= 0 ? WorldConstants.WorldDistancePerDifficulty : distancePerDifficulty;
            _generator = new CoordinateWorldGenerator(seed, surfaceBaseline, surfaceVariation, anchorSpan, worldFloor);
            _terrainStamps = new CoordinateTerrainStampField(_generator, worldFloor,
                cityBlockLengthCells, cityBlockFlatHalfWidthCells);
            _biomeField = new WorldBiomeField(seed, biomeCount, biomeRegionSpanChunks);
        }

        public WorldChunkData GenerateChunk(WorldChunkCoord coordinate)
        {
            WorldChunkKind kind = _terrainStamps.GetChunkKind(coordinate);
            var tiles = new TileId[WorldChunkCoord.Size * WorldChunkCoord.Size];
            for (int localY = 0; localY < WorldChunkCoord.Size; localY++)
                for (int localX = 0; localX < WorldChunkCoord.Size; localX++)
                {
                    int worldX = coordinate.MinCellX + localX;
                    int worldY = coordinate.MinCellY + localY;
                    TileId tile = worldY <= _worldFloor
                        ? TileId.Void
                        : worldY <= _terrainStamps.SurfaceHeightAt(worldX) ? TileId.Ground : TileId.Sky;
                    tiles[localY * WorldChunkCoord.Size + localX] = tile;
                }

            int biome = _biomeField.GetBiome(coordinate.X);
            ulong hash = WorldHash.Combine(_terrainStamps.ContentHash(_seed, coordinate), biome);
            var spawns = CreateSpawnLocations(coordinate, kind, hash);
            float distance = coordinate.MinCellX - _surfaceBaseline;
            float difficulty = distance <= 0f ? 0f : distance / _distancePerDifficulty;
            hash = WorldHash.Combine(hash, (int)kind);
            foreach (SpawnLocation spawn in spawns)
            {
                hash = WorldHash.Combine(hash, (int)spawn.Kind);
                hash = WorldHash.Combine(hash, spawn.SlotIndex);
                hash = WorldHash.Combine(hash, spawn.ContentKindIndex);
                hash = WorldHash.Combine(hash, (int)(spawn.NormalizedX * 65535f));
            }
            return new WorldChunkData(coordinate, kind, biome, _worldFloor, difficulty, hash, tiles, spawns);
        }

        public TileId GetTileAtCell(int worldCellX, int worldCellY)
        { return _terrainStamps.GetTileAtCell(worldCellX, worldCellY); }

        /// <summary>Returns ground occupancy from the final stamped tile lookup.</summary>
        public bool IsFilled(int x, int y)
        { return GetTileAtCell(x, y) == TileId.Ground; }

        public int GetSurfaceHeight(int worldCellX) { return _terrainStamps.SurfaceHeightAt(worldCellX); }
        /// <summary>Validates the selected artwork's entire horizontal extent against the owning biome.</summary>
        public bool CanPlaceBuilding(int biome, float minimumWorldX, float maximumWorldX)
        { return CoordinateBuildingPlacement.FitsBiome(_biomeField, biome, minimumWorldX, maximumWorldX); }
        public WorldChunkKind GetChunkKind(WorldChunkCoord coordinate) { return _terrainStamps.GetChunkKind(coordinate); }

        SpawnLocation[] CreateSpawnLocations(WorldChunkCoord coordinate, WorldChunkKind kind, ulong hash)
        {
            if (kind != WorldChunkKind.Surface) return new SpawnLocation[0];
            if (!TryChooseSurfaceCell(coordinate, hash, out int surfaceCellX)) return new SpawnLocation[0];
            var spawns = new List<SpawnLocation>(3);
            int enemyPhase = (int)(_seed % 3UL);
            float normalizedX = (coordinate.LocalIndexX(surfaceCellX) + 0.5f) / WorldChunkCoord.Size;
            if (PositiveMod(coordinate.X + enemyPhase, 3) == 0)
            {
                int enemyKind = PositiveMod(WorldChunkCoord.FloorDiv(coordinate.X + enemyPhase, 3), 3);
                spawns.Add(new SpawnLocation(SpawnLocationKind.Enemy, normalizedX, 0, enemyKind, -1));
            }
            if ((hash >> 16) % 7UL == 0UL)
            {
                spawns.Add(new SpawnLocation(SpawnLocationKind.PowerUp, normalizedX, 0, (int)((hash >> 32) % 3UL), -1));
            }
            int buildingCenterX = coordinate.MinCellX + WorldChunkCoord.Size / 2;
            if (_terrainStamps.IsCityBlockCenter(buildingCenterX))
            {
                int buildingKind = (hash >> 8) % 5UL == 0UL
                    ? (int)BuildingType.PizzaParlor
                    : (int)BuildingType.Generic;
                int accent = (int)(hash % WorldConstants.AccentsPerBiome);
                float buildingNormalizedX = (coordinate.LocalIndexX(buildingCenterX) + 0.5f) / WorldChunkCoord.Size;
                spawns.Add(new SpawnLocation(SpawnLocationKind.BuildingAnchor,
                    buildingNormalizedX, accent, buildingKind, -1));
            }
            return spawns.ToArray();
        }

        bool TryChooseSurfaceCell(WorldChunkCoord coordinate, ulong hash, out int surfaceCellX)
        {
            int count = 0;
            for (int x = coordinate.MinCellX; x <= coordinate.MaxCellX; x++)
                if (WorldChunkCoord.FloorDiv(_terrainStamps.SurfaceHeightAt(x), WorldChunkCoord.Size) == coordinate.Y) count++;
            surfaceCellX = coordinate.MinCellX;
            if (count == 0) return false;
            int selected = (int)(hash % (ulong)count);
            for (int x = coordinate.MinCellX; x <= coordinate.MaxCellX; x++)
                if (WorldChunkCoord.FloorDiv(_terrainStamps.SurfaceHeightAt(x), WorldChunkCoord.Size) == coordinate.Y && selected-- == 0)
                { surfaceCellX = x; return true; }
            return false;
        }

        static int PositiveMod(int value, int divisor)
        { return ((value % divisor) + divisor) % divisor; }
    }
}
