namespace EscapeThe90s.WorldGen
{
    /// <summary>Desired loaded range and player position for one streamer update.</summary>
    public readonly struct ChunkStreamWindow
    {
        public readonly int Center, FirstChunk, LastChunk;
        public readonly float StreetLocalX;
        public ChunkStreamWindow(int center, int firstChunk, int lastChunk, float streetLocalX)
        { Center = center; FirstChunk = firstChunk; LastChunk = lastChunk; StreetLocalX = streetLocalX; }
    }
}
