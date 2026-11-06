namespace TL2_WorldData
{
    /// <summary>
    /// Tile layers of a chunk. APPEND-ONLY: never reorder or renumber. The numeric values
    /// feed the chunk hash, the array layout and save-file keys.
    /// </summary>
    public enum TileLayer : byte
    {
        Background = 0,   // decoration behind the action. Never read by gameplay.
        Foreground = 1,   // terrain, walls, cover, lifts. Everything gameplay reads.
        // Future (append only): Overlay = 2, Liquid = 3
    }

    public static class Layers
    {
        /// <summary>Number of layers stored per chunk. Bump when appending to TileLayer.</summary>
        public const int Count = 2;
    }
}