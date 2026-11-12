namespace EscapeThe90s.WorldGen
{
    /// <summary>Applies deterministic city-block pads and connecting ramps to the seeded surface field.</summary>
    public sealed class CoordinateTerrainStampField
    {
        readonly CoordinateWorldGenerator _baseSurface;
        readonly int _worldFloor;
        readonly int _blockLengthCells;
        readonly int _flatHalfWidthCells;
        readonly int _buildingFootprintHalfWidthCells;

        /// <summary>Creates stable city lots with configured flat widths and ramps connecting to surrounding terrain.</summary>
        public CoordinateTerrainStampField(CoordinateWorldGenerator baseSurface, int worldFloor,
            int blockLengthCells = WorldConstants.WorldCityBlockLengthChunks * WorldChunkCoord.Size,
            int flatHalfWidthCells = WorldConstants.WorldCityBlockFlatHalfWidthCells,
            int buildingFootprintHalfWidthCells = WorldConstants.WorldBuildingFootprintHalfWidthCells)
        {
            _baseSurface = baseSurface;
            _worldFloor = worldFloor;
            int blockChunks = blockLengthCells / WorldChunkCoord.Size;
            if (blockChunks < WorldConstants.WorldCityBlockLengthChunks) blockChunks = WorldConstants.WorldCityBlockLengthChunks;
            if (blockChunks % 2 == 0) blockChunks++;
            _blockLengthCells = blockChunks * WorldChunkCoord.Size;
            int maximumFlatHalfWidth = _blockLengthCells / 2 - 1;
            int configuredFootprint = buildingFootprintHalfWidthCells < 0
                ? WorldConstants.WorldBuildingFootprintHalfWidthCells
                : buildingFootprintHalfWidthCells;
            _buildingFootprintHalfWidthCells = configuredFootprint > maximumFlatHalfWidth
                ? maximumFlatHalfWidth
                : configuredFootprint;
            _flatHalfWidthCells = flatHalfWidthCells < _buildingFootprintHalfWidthCells
                ? _buildingFootprintHalfWidthCells
                : flatHalfWidthCells > maximumFlatHalfWidth ? maximumFlatHalfWidth : flatHalfWidthCells;
        }

        /// <summary>Returns the stamped surface height at a world cell.</summary>
        public int SurfaceHeightAt(int worldCellX)
        {
            int segment = WorldChunkCoord.FloorDiv(worldCellX, _blockLengthCells);
            int start = segment * _blockLengthCells;
            int flatStart = start + _flatHalfWidthCells;
            int flatEnd = start + _blockLengthCells - _flatHalfWidthCells;
            int end = start + _blockLengthCells;
            int center = start + _blockLengthCells / 2;
            int flatHeight = _baseSurface.SurfaceHeightAt(center);

            if (worldCellX >= flatStart && worldCellX <= flatEnd) return flatHeight;
            if (worldCellX < flatStart)
            {
                int baseHeight = _baseSurface.SurfaceHeightAt(start);
                return Interpolate(baseHeight, flatHeight, worldCellX - start, _flatHalfWidthCells);
            }

            int endHeight = _baseSurface.SurfaceHeightAt(end);
            return Interpolate(flatHeight, endHeight, worldCellX - flatEnd, _flatHalfWidthCells);
        }

        /// <summary>Checks whether every cell in a centered run has the same surface height.</summary>
        public bool IsFlatSurfaceRun(int centerCellX, int halfWidthCells)
        {
            if (halfWidthCells < 0) return false;
            int expectedHeight = SurfaceHeightAt(centerCellX);
            int firstCell = centerCellX - halfWidthCells;
            int lastCell = centerCellX + halfWidthCells;
            for (int x = firstCell; x <= lastCell; x++)
                if (SurfaceHeightAt(x) != expectedHeight) return false;
            return true;
        }

        /// <summary>Checks whether a cell is the center of a flat, building-sized city lot.</summary>
        public bool IsCityBlockCenter(int worldCellX)
        {
            int segment = WorldChunkCoord.FloorDiv(worldCellX, _blockLengthCells);
            int center = segment * _blockLengthCells + _blockLengthCells / 2;
            return worldCellX == center && IsFlatSurfaceRun(center, _buildingFootprintHalfWidthCells);
        }

        /// <summary>Returns whether a world cell contains solid terrain above the world floor.</summary>
        public bool IsFilled(int worldCellX, int worldCellY)
        { return worldCellY > _worldFloor && worldCellY <= SurfaceHeightAt(worldCellX); }

        /// <summary>Returns the tile meaning at a world coordinate.</summary>
        public TileId GetTileAtCell(int worldCellX, int worldCellY)
        {
            if (worldCellY <= _worldFloor) return TileId.Void;
            return worldCellY <= SurfaceHeightAt(worldCellX) ? TileId.Ground : TileId.Sky;
        }

        /// <summary>Classifies a chunk from its stamped surface and the configured world floor.</summary>
        public WorldChunkKind GetChunkKind(WorldChunkCoord coordinate)
        {
            int minimumSurface = int.MaxValue;
            int maximumSurface = int.MinValue;
            for (int x = coordinate.MinCellX; x <= coordinate.MaxCellX; x++)
            {
                int height = SurfaceHeightAt(x);
                if (height < minimumSurface) minimumSurface = height;
                if (height > maximumSurface) maximumSurface = height;
            }

            if (coordinate.MaxCellY <= _worldFloor) return WorldChunkKind.Void;
            if (coordinate.MinCellY <= _worldFloor) return WorldChunkKind.Surface;
            if (coordinate.MinCellY > maximumSurface) return WorldChunkKind.Sky;
            if (coordinate.MaxCellY <= minimumSurface) return WorldChunkKind.Solid;
            return WorldChunkKind.Surface;
        }

        /// <summary>Hashes the final stamped surface for one chunk in generation-order-independent fashion.</summary>
        public ulong ContentHash(ulong seed, WorldChunkCoord coordinate)
        {
            ulong hash = WorldHash.Combine(WorldHash.Combine(seed, coordinate.X), coordinate.Y);
            for (int x = coordinate.MinCellX; x <= coordinate.MaxCellX; x++)
                hash = WorldHash.Combine(hash, SurfaceHeightAt(x));
            return hash;
        }

        static int Interpolate(int from, int to, int offset, int length)
        {
            int numerator = (to - from) * offset;
            int rounded = numerator >= 0
                ? (numerator + length / 2) / length
                : (numerator - length / 2) / length;
            return from + rounded;
        }
    }
}
