using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>A solid axis-aligned box in chunk-local cell coordinates (x from the chunk's left edge, y up from the surface).</summary>
    public struct ColliderBox
    {
        public int X0, X1, Y0, Y1;
        public ColliderBox(int x0, int x1, int y0, int y1) { X0 = x0; X1 = x1; Y0 = y0; Y1 = y1; }
    }

    /// <summary>
    /// Physics shapes come from the DATA grid (exact cells), not from tile art, so collision never depends on how the dual-grid
    /// tiles look. One deep slab under the street plus one box per run of equal-height bump columns.
    /// </summary>
    public static class TerrainColliders
    {
        public static List<ColliderBox> ForChunk(StreetTerrain terrain, int chunkIndex)
        {
            int w = WorldConstants.ChunkWidthCells;
            int x0 = chunkIndex * w;
            var boxes = new List<ColliderBox>();
            boxes.Add(new ColliderBox(0, w, -WorldConstants.SlabDepthCells, 0));

            int lx = 0;
            while (lx < w)
            {
                int h = terrain.HeightAt(x0 + lx);
                if (h <= 0) { lx++; continue; }
                int start = lx;
                while (lx < w && terrain.HeightAt(x0 + lx) == h) lx++;
                boxes.Add(new ColliderBox(start, lx, 0, h));
            }
            return boxes;
        }
    }
}
