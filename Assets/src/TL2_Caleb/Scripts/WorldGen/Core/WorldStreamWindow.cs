namespace EscapeThe90s.WorldGen
{
    /// <summary>Inclusive rectangular request window for coordinate chunk streaming.</summary>
    public readonly struct WorldStreamWindow
    {
        public readonly WorldChunkCoord Center;
        public readonly int MinX;
        public readonly int MaxX;
        public readonly int MinY;
        public readonly int MaxY;
        public readonly int DirectionX;
        public readonly int LookaheadChunks;
        public readonly bool IsCapped;

        public WorldStreamWindow(WorldChunkCoord center, int minX, int maxX, int minY, int maxY,
            int directionX, int lookaheadChunks, bool isCapped)
        {
            Center = center;
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            DirectionX = directionX;
            LookaheadChunks = lookaheadChunks;
            IsCapped = isCapped;
        }

        public bool Contains(WorldChunkCoord coordinate)
        { return coordinate.X >= MinX && coordinate.X <= MaxX && coordinate.Y >= MinY && coordinate.Y <= MaxY; }
    }
}
