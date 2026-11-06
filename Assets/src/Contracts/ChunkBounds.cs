namespace EscapeThe90s.Contracts
{
    /// <summary>Axis-aligned world-space bounds for a generated chunk.</summary>
    public readonly struct ChunkBounds
    {
        public readonly float MinX, MinY, MaxX, MaxY;
        public ChunkBounds(float minX, float minY, float maxX, float maxY)
        { MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY; }
    }
}
