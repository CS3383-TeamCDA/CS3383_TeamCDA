namespace EscapeThe90s.WorldGen
{
    /// <summary>Main-thread GC allocation event counts and CPU ticks for one streaming step, excluding measurement callers.</summary>
    public readonly struct CoordinateStreamingSample
    {
        public readonly long TerrainAllocationEvents;
        public readonly long ContentAllocationEvents;
        public readonly long TerrainTicks;
        public readonly long ContentTicks;
        public CoordinateStreamingSample(long terrainAllocationEvents, long contentAllocationEvents, long terrainTicks, long contentTicks)
        { TerrainAllocationEvents = terrainAllocationEvents; ContentAllocationEvents = contentAllocationEvents; TerrainTicks = terrainTicks; ContentTicks = contentTicks; }
    }
}
