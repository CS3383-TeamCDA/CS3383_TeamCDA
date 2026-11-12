using System;

namespace EscapeThe90s.WorldGen
{
    /// <summary>World-space placement rules for coordinate buildings; cells are centered on integer coordinates.</summary>
    public static class CoordinateBuildingPlacement
    {
        /// <summary>Aligns the sprite's local bottom with the final lot surface.</summary>
        public static float OriginY(float surfaceWorldY, float spriteMinimumY)
        { return surfaceWorldY - spriteMinimumY; }

        /// <summary>Checks every occupied cell column; touching a column edge does not occupy the next column.</summary>
        public static bool FitsBiome(WorldBiomeField field, int biome, float minimumWorldX, float maximumWorldX)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (float.IsNaN(minimumWorldX) || float.IsNaN(maximumWorldX) ||
                float.IsInfinity(minimumWorldX) || float.IsInfinity(maximumWorldX) ||
                maximumWorldX <= minimumWorldX) return false;
            int firstCell = (int)Math.Floor(minimumWorldX + 0.5d);
            int lastCell = (int)Math.Ceiling(maximumWorldX + 0.5d) - 1;
            for (int cellX = firstCell; cellX <= lastCell; cellX++)
                if (field.GetBiome(WorldChunkCoord.FloorDiv(cellX, WorldChunkCoord.Size)) != biome) return false;
            return true;
        }
    }
}
