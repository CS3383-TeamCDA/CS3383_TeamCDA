namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Phase 0 numbers everything depends on. PLACEHOLDERS: confirm with art and the team,
    /// and change them before content is built on top (changing later reshuffles every seed).
    /// </summary>
    public static class WorldConstants
    {
        public const int CellPixels = 16;          // one data-grid cell, in pixels
        public const int ChunkWidthCells = 32;     // chunk width in cells (=512 px at 16 px cells)
        public const int EndSafeChunks = 1;        // chunks at each street end: no buildings, no enemies
        public const int MinStreetChunks = 6;      // shortest legal street
        public const int DistancePerTier = 200;    // world units from origin per difficulty tier
        public const int BiomeCount = 5;           // one per arcade game

        public static int ChunkWidthPixels { get { return CellPixels * ChunkWidthCells; } }
    }

    /// <summary>RNG stream salts. One stream per concern so adding rules to one never shifts another.</summary>
    public static class Salt
    {
        public const int Placement = 1;   // which building goes in which chunk
        public const int ChunkLook = 2;   // variant / accent / flip
        public const int Enemies = 3;     // enemy slots
    }
}
