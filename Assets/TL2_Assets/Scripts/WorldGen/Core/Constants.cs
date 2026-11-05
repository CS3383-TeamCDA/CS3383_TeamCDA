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
        public const int MinStreetChunks = 4;      // shortest legal street (two safe ends + two playable chunks)
        public const int DistancePerTier = 200;    // world units from origin per difficulty tier
        public const int BiomeCount = 5;           // one per arcade game
        public const int AccentsPerBiome = 3;      // palette rows per biome (palette.png has BiomeCount*AccentsPerBiome rows)
        public const int GroundDepthCells = 3;     // visible ground rows below the street surface
        public const int MaxBumpHeight = 2;        // tallest curb/step, in cells
        public const int SlabDepthCells = 8;       // collider depth under the street surface (visible ground is shallower)
        public const int RegionSizeNodes = 12;     // one coarse cell is RegionSizeNodes x RegionSizeNodes street-grid nodes (about one region)
        public const int MinConnectionSpacing = 3; // surface connections between two regions are at least this far apart (Manhattan, in nodes)
        public static readonly int[] LinkCountWeights = { 25, 40, 25, 10 };   // chance weights for 0,1,2,3 surface connections per adjacent pair
        public const int ViewWidthPixels = 640;    // target resolution
        public const int ViewHeightPixels = 360;

        public static int ChunkWidthPixels { get { return CellPixels * ChunkWidthCells; } }
    }

    /// <summary>RNG stream salts. One stream per concern so adding rules to one never shifts another.</summary>
    public static class Salt
    {
        public const int Placement = 1;   // which building goes in which chunk
        public const int ChunkLook = 2;   // variant / accent / flip
        public const int Enemies = 3;     // enemy slots
        public const int Terrain = 4;     // curbs / steps
        public const int Site = 5;        // region site jitter
        public const int Link = 6;        // region-pair connections
        public const int Interior = 7;    // street network inside a region
    }
}
