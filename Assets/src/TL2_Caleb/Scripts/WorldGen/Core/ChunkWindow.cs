using System;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Which chunks should exist around a position. Pure functions so the streaming rule is testable.</summary>
    public static class ChunkWindow
    {
        /// <summary>Chunk containing world x (in cells/units). Clamped to the street.</summary>
        public static int ChunkIndexAt(float worldX, int chunkCount)
        {
            int i = (int)Math.Floor(worldX / WorldConstants.ChunkWidthCells);
            if (i < 0) return 0;
            if (i > chunkCount - 1) return chunkCount - 1;
            return i;
        }

        public static bool ShouldLoad(int index, int center, int radius)
        {
            return Math.Abs(index - center) <= radius;
        }

        /// <summary>Unload one chunk later than load, so walking back and forth on a border never thrashes.</summary>
        public static bool ShouldUnload(int index, int center, int radius)
        {
            return Math.Abs(index - center) > radius + 1;
        }
    }
}
