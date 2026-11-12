namespace EscapeThe90s.WorldGen
{
    /// <summary>A street segment's traversal orientation and global chunk range within an endless run.</summary>
    public sealed class RunPathStreet
    {
        internal RunPathStreet(int index, long firstGlobalChunk, WorldStreet street,
            Int2 entryNode, Int2 exitNode, bool reversed)
        {
            Index = index;
            FirstGlobalChunk = firstGlobalChunk;
            Street = street;
            EntryNode = entryNode;
            ExitNode = exitNode;
            Reversed = reversed;
            EndGlobalChunkExclusive = firstGlobalChunk + street.Layout.ChunkCount;
        }

        public int Index { get; private set; }
        public long FirstGlobalChunk { get; private set; }
        public long EndGlobalChunkExclusive { get; private set; }
        public WorldStreet Street { get; private set; }
        public Int2 EntryNode { get; private set; }
        public Int2 ExitNode { get; private set; }
        public bool Reversed { get; private set; }
    }
}
