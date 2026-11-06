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
    /// tiles look. Adjacent columns with the same surface height share a deep box; gap columns have no collider.
    /// </summary>
    public static class TerrainColliders
    {
        public static List<ColliderBox> ForChunk(StreetTerrain terrain, int chunkIndex)
        {
            int w = WorldConstants.ChunkWidthCells;
            int x0 = chunkIndex * w;
            var boxes = new List<ColliderBox>();
            int lx = 0;
            while (lx < w)
            {
                int worldX = x0 + lx;
                if (terrain.IsGap(worldX)) { lx++; continue; }
                int h = terrain.HeightAt(worldX);
                int start = lx;
                while (lx < w && !terrain.IsGap(x0 + lx) && terrain.HeightAt(x0 + lx) == h) lx++;
                int bottom = System.Math.Min(-WorldConstants.SlabDepthCells, h - WorldConstants.SlabDepthCells);
                boxes.Add(new ColliderBox(start, lx, bottom, h));
            }
            return boxes;
        }
    }
}
