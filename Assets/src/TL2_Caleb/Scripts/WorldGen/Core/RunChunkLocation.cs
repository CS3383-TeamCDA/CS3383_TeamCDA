namespace EscapeThe90s.WorldGen
{
    /// <summary>Identifies a chunk's location in the global run path and in its source street.</summary>
    public readonly struct RunChunkLocation
    {
        public readonly long GlobalChunkIndex;
        public readonly RunPathStreet Segment;
        public readonly int StreetChunkIndex;
        public bool Reversed { get { return Segment != null && Segment.Reversed; } }

        internal RunChunkLocation(long globalChunkIndex, RunPathStreet segment, int streetChunkIndex)
        {
            GlobalChunkIndex = globalChunkIndex;
            Segment = segment;
            StreetChunkIndex = streetChunkIndex;
        }
    }
}
