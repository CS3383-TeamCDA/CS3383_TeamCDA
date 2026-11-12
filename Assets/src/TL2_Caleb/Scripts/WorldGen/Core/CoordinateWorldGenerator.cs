namespace EscapeThe90s.WorldGen
{
    /// <summary>Pure deterministic terrain-height field for the side-scroller coordinate world.</summary>
    public sealed class CoordinateWorldGenerator : ICellSource
    {
        readonly ulong _seed;
        readonly int _surfaceBaseline;
        readonly int _surfaceVariation;
        readonly int _anchorSpan;
        readonly int _worldFloor;

        public CoordinateWorldGenerator(ulong seed)
            : this(seed, WorldConstants.WorldSurfaceBaseline, WorldConstants.WorldSurfaceVariationCells,
                WorldConstants.WorldSurfaceAnchorSpanCells, WorldConstants.WorldFloorCellY) { }

        public CoordinateWorldGenerator(ulong seed, int surfaceBaseline, int surfaceVariation,
            int anchorSpan, int worldFloor)
        {
            _seed = seed;
            _surfaceBaseline = surfaceBaseline;
            _surfaceVariation = surfaceVariation < 0 ? 0 : surfaceVariation;
            _anchorSpan = anchorSpan < WorldChunkCoord.Size ? WorldConstants.WorldSurfaceAnchorSpanCells : anchorSpan;
            _worldFloor = worldFloor;
        }

        public int SurfaceHeightAt(int worldX)
        {
            int span = _anchorSpan;
            int segment = WorldChunkCoord.FloorDiv(worldX, span);
            int offset = worldX - segment * span;
            int startHeight = AnchorHeight(segment);
            int endHeight = AnchorHeight(segment + 1);
            int easedOffset = offset * offset * (3 * span - 2 * offset) / (span * span);
            return startHeight + (endHeight - startHeight) * easedOffset / span;
        }

        int AnchorHeight(int segment)
        {
            ulong hash = WorldHash.Combine(WorldHash.Combine(_seed, 0x574F524C), segment);
            ulong variationRange = (ulong)(_surfaceVariation * 2 + 1);
            int variation = (int)(hash % variationRange);
            return _surfaceBaseline - _surfaceVariation + variation;
        }

        public bool IsFilled(int x, int y)
        { return y > _worldFloor && y <= SurfaceHeightAt(x); }

        public WorldChunkKind GetChunkKind(WorldChunkCoord coordinate)
        {
            GetSurfaceHeightRange(coordinate, out int minimumSurface, out int maximumSurface);
            if (coordinate.MaxCellY <= _worldFloor) return WorldChunkKind.Void;
            if (coordinate.MinCellY <= _worldFloor) return WorldChunkKind.Surface;
            if (coordinate.MinCellY > maximumSurface) return WorldChunkKind.Sky;
            if (coordinate.MaxCellY <= minimumSurface)
                return WorldChunkKind.Solid;
            return WorldChunkKind.Surface;
        }

        public void GetSurfaceHeightRange(WorldChunkCoord coordinate, out int minimum, out int maximum)
        {
            minimum = int.MaxValue;
            maximum = int.MinValue;
            for (int x = coordinate.MinCellX; x <= coordinate.MaxCellX; x++)
            {
                int height = SurfaceHeightAt(x);
                if (height < minimum) minimum = height;
                if (height > maximum) maximum = height;
            }
        }

        public ulong ContentHash(WorldChunkCoord coordinate)
        {
            ulong hash = WorldHash.Combine(WorldHash.Combine(_seed, coordinate.X), coordinate.Y);
            for (int x = coordinate.MinCellX; x <= coordinate.MaxCellX; x++)
                hash = WorldHash.Combine(hash, SurfaceHeightAt(x));
            return hash;
        }
    }
}
