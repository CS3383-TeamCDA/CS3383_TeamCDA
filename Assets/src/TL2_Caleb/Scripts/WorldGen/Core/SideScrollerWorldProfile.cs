using System;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    /// <summary>World profile for an endless horizontal run with vertical ground and sky fill.</summary>
    public sealed class SideScrollerWorldProfile : IWorldProfile
    {
        static readonly EnemyType[] EnemyRotation =
        {
            EnemyType.Ground,
            EnemyType.Flying,
            EnemyType.Turret,
            EnemyType.TVClaw,
            EnemyType.GrenadeBerserker
        };

        readonly ulong _seed;
        readonly CoordinateWorldGenerator _generator;
        readonly CoordinateTerrainStampField _terrainStamps;
        readonly WorldBiomeField _biomeField;
        readonly int _surfaceBaseline;
        readonly int _worldFloor;
        readonly int _distancePerDifficulty;
        readonly int _leftOpeningAccent;
        readonly int _rightOpeningAccent;

        public SideScrollerWorldProfile(ulong seed, int surfaceBaseline = WorldConstants.WorldSurfaceBaseline,
            int surfaceVariation = WorldConstants.WorldSurfaceVariationCells,
            int anchorSpan = WorldConstants.WorldSurfaceAnchorSpanCells,
            int worldFloor = WorldConstants.WorldFloorCellY,
            int distancePerDifficulty = WorldConstants.WorldDistancePerDifficulty,
            int biomeCount = WorldConstants.BiomeCount,
            int biomeRegionSpanChunks = WorldConstants.WorldBiomeRegionSpanChunks,
            int cityBlockLengthCells = WorldConstants.WorldCityBlockLengthChunks * WorldChunkCoord.Size,
            int cityBlockFlatHalfWidthCells = WorldConstants.WorldCityBlockFlatHalfWidthCells,
            int gapChancePercent = WorldConstants.WorldGapChancePercent,
            int gapApproachCells = WorldConstants.WorldGapApproachCells,
            int spawnSafeBlocks = WorldConstants.WorldSpawnSafeBlocks)
        {
            _seed = seed;
            _surfaceBaseline = surfaceBaseline;
            _worldFloor = worldFloor;
            _distancePerDifficulty = distancePerDifficulty <= 0 ? WorldConstants.WorldDistancePerDifficulty : distancePerDifficulty;
            _generator = new CoordinateWorldGenerator(seed, surfaceBaseline, surfaceVariation, anchorSpan, worldFloor);
            _terrainStamps = new CoordinateTerrainStampField(_generator, worldFloor,
                cityBlockLengthCells, cityBlockFlatHalfWidthCells, seed: seed,
                gapChancePercent: gapChancePercent, gapApproachCells: gapApproachCells, spawnSafeBlocks: spawnSafeBlocks);
            _biomeField = new WorldBiomeField(seed, biomeCount, biomeRegionSpanChunks);
            _leftOpeningAccent = GetOriginalLotAccent(-WorldConstants.WorldSpawnSafeBlocks);
            _rightOpeningAccent = GetOriginalLotAccent(WorldConstants.WorldSpawnSafeBlocks - 1);
        }

        public WorldChunkData GenerateChunk(WorldChunkCoord coordinate)
        {
            WorldChunkKind kind = _terrainStamps.GetChunkKind(coordinate);
            var groundEnds = new byte[WorldChunkCoord.Size];
            for (int localX = 0; localX < groundEnds.Length; localX++)
            {
                int x = coordinate.MinCellX + localX;
                if (_terrainStamps.IsGapColumn(x)) continue;
                int surface = _terrainStamps.SurfaceHeightAt(x);
                if (surface >= coordinate.MinCellY)
                    groundEnds[localX] = surface >= coordinate.MaxCellY
                        ? (byte)WorldChunkCoord.Size : (byte)(surface - coordinate.MinCellY + 1);
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
            return new WorldChunkData(coordinate, kind, biome, _worldFloor, difficulty, hash, groundEnds, spawns,
                _terrainStamps.GetTerrainKind(coordinate));
        }

        public TileId GetTileAtCell(int worldCellX, int worldCellY)
        { return _terrainStamps.GetTileAtCell(worldCellX, worldCellY); }

        /// <summary>Returns ground occupancy from the final stamped tile lookup.</summary>
        public bool IsFilled(int x, int y)
        { return GetTileAtCell(x, y) == TileId.Ground; }

        public int GetSurfaceHeight(int worldCellX) { return _terrainStamps.SurfaceHeightAt(worldCellX); }
        /// <summary>Returns whether a column is a gap; surface height remains the nominal takeoff/landing elevation.</summary>
        public bool IsGapColumn(int worldCellX) { return _terrainStamps.IsGapColumn(worldCellX); }
        /// <summary>Validates the selected artwork's entire horizontal extent against the owning biome.</summary>
        public bool CanPlaceBuilding(int biome, float minimumWorldX, float maximumWorldX)
        { return CoordinateBuildingPlacement.FitsBiome(_biomeField, biome, minimumWorldX, maximumWorldX); }
        public WorldChunkKind GetChunkKind(WorldChunkCoord coordinate) { return _terrainStamps.GetChunkKind(coordinate); }

        SpawnLocation[] CreateSpawnLocations(WorldChunkCoord coordinate, WorldChunkKind kind, ulong hash)
        {
            if (kind != WorldChunkKind.Surface) return Array.Empty<SpawnLocation>();
            if (!TryChooseSurfaceCell(coordinate, hash, out int surfaceCellX)) return Array.Empty<SpawnLocation>();
            int enemyPhase = (int)(_seed % 3UL);
            float normalizedX = (coordinate.LocalIndexX(surfaceCellX) + 0.5f) / WorldChunkCoord.Size;
            bool hasEnemy = PositiveMod(coordinate.X + enemyPhase, 3) == 0;
            bool hasPowerUp = (hash >> 16) % 7UL == 0UL;
            int buildingCenterX = coordinate.MinCellX + WorldChunkCoord.Size / 2;
            bool hasBuilding = _terrainStamps.IsCityBlockCenter(buildingCenterX);
            int count = (hasEnemy ? 1 : 0) + (hasPowerUp ? 1 : 0) + (hasBuilding ? 1 : 0);
            if (count == 0) return Array.Empty<SpawnLocation>();
            var spawns = new SpawnLocation[count];
            int index = 0;
            if (hasEnemy)
            {
                int enemyIndex = PositiveMod(
                    WorldChunkCoord.FloorDiv(coordinate.X + enemyPhase, 3), EnemyRotation.Length);
                spawns[index++] = new SpawnLocation(
                    SpawnLocationKind.Enemy, normalizedX, 0, (int)EnemyRotation[enemyIndex], -1);
            }
            if (hasPowerUp)
            {
                spawns[index++] = new SpawnLocation(SpawnLocationKind.PowerUp, normalizedX, 0, (int)((hash >> 32) % 3UL), -1);
            }
            if (hasBuilding)
            {
                int buildingKind = (hash >> 8) % 5UL == 0UL
                    ? (int)BuildingType.PizzaParlor
                    : (int)BuildingType.Generic;
                int accent = (int)(hash % WorldConstants.AccentsPerBiome);
                int block = _terrainStamps.GetCityBlockIndex(buildingCenterX);
                // Preserve the original opening lots. Beyond them, each group of four lots
                // has one restaurant in a seeded interior slot, spaced away from its neighbors.
                if (block < -WorldConstants.WorldSpawnSafeBlocks || block >= WorldConstants.WorldSpawnSafeBlocks)
                {
                    const int lotsPerGroup = 4;
                    int group = WorldChunkCoord.FloorDiv(block, lotsPerGroup);
                    ulong placement = WorldHash.Combine(WorldHash.Combine(_seed, Salt.Placement), group);
                    int restaurantSlot = 1 + (int)(placement % 2UL);
                    buildingKind = PositiveMod(block, lotsPerGroup) == restaurantSlot
                        ? (int)BuildingType.PizzaParlor : (int)BuildingType.Generic;
                    int openingBlock = block < 0 ? -WorldConstants.WorldSpawnSafeBlocks : WorldConstants.WorldSpawnSafeBlocks - 1;
                    int openingAccent = block < 0 ? _leftOpeningAccent : _rightOpeningAccent;
                    accent = PositiveMod(block - openingBlock + openingAccent, WorldConstants.AccentsPerBiome);
                }
                float buildingNormalizedX = (coordinate.LocalIndexX(buildingCenterX) + 0.5f) / WorldChunkCoord.Size;
                spawns[index] = new SpawnLocation(SpawnLocationKind.BuildingAnchor,
                    buildingNormalizedX, accent, buildingKind, -1);
            }
            return spawns;
        }

        private int GetOriginalLotAccent(int block)
        {
            int center = _terrainStamps.GetCityBlockCenter(block);
            var coordinate = WorldChunkCoord.AtCell(center, _terrainStamps.SurfaceHeightAt(center));
            ulong hash = WorldHash.Combine(_terrainStamps.ContentHash(_seed, coordinate), _biomeField.GetBiome(coordinate.X));
            return (int)(hash % WorldConstants.AccentsPerBiome);
        }

        bool TryChooseSurfaceCell(WorldChunkCoord coordinate, ulong hash, out int surfaceCellX)
        {
            int count = 0;
            for (int x = coordinate.MinCellX; x <= coordinate.MaxCellX; x++)
                if (!_terrainStamps.IsGapColumn(x) && WorldChunkCoord.FloorDiv(_terrainStamps.SurfaceHeightAt(x), WorldChunkCoord.Size) == coordinate.Y) count++;
            surfaceCellX = coordinate.MinCellX;
            if (count == 0) return false;
            int selected = (int)(hash % (ulong)count);
            for (int x = coordinate.MinCellX; x <= coordinate.MaxCellX; x++)
                if (!_terrainStamps.IsGapColumn(x) && WorldChunkCoord.FloorDiv(_terrainStamps.SurfaceHeightAt(x), WorldChunkCoord.Size) == coordinate.Y && selected-- == 0)
                { surfaceCellX = x; return true; }
            return false;
        }

        static int PositiveMod(int value, int divisor)
        { return ((value % divisor) + divisor) % divisor; }
    }
}
