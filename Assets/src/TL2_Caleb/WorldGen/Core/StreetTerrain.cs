using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Street-wide ground data (the "data grid" of the dual grid). Cell x runs along the street
    /// (chunk i owns cells i*ChunkWidthCells ..), y = 0 is the walking surface; everything below is solid.
    /// Optional bumps (curbs/steps) appear only in chunks with no building.
    /// Being street-wide (not per chunk) is what makes chunk seams render correctly: a tile on a chunk
    /// border simply reads cells from the neighbouring chunk.
    /// </summary>
    public sealed class StreetTerrain : ICellSource
    {
        readonly int[] _height;       // surface height per column; 0 = flat
        public int WidthCells { get; private set; }
        public int MaxHeight { get; private set; }

        public StreetTerrain(ulong seed, StreetLayout layout, bool bumps)
        {
            WidthCells = layout.ChunkCount * WorldConstants.ChunkWidthCells;
            _height = new int[WidthCells];
            if (!bumps) return;

            int w = WorldConstants.ChunkWidthCells;
            for (int i = 0; i < layout.ChunkCount; i++)
            {
                ChunkData c = layout.Chunks[i];
                if (c.IsSafe || c.Building != BuildingType.None) continue;
                var rng = DeterministicRng.Create(seed, layout.Id, i, Salt.Terrain);
                if (!rng.ChancePercent(45)) continue;
                int bw = rng.Range(4, 9);
                int bh = rng.Range(1, WorldConstants.MaxBumpHeight + 1);
                int x0 = i * w + rng.Range(2, w - bw - 1);
                for (int x = x0; x < x0 + bw; x++) _height[x] = bh;
                if (bh > MaxHeight) MaxHeight = bh;
            }
        }

        /// <summary>Beyond the street ends the ground continues flat (clamped), so ends never show a gap.</summary>
        public bool IsFilled(int x, int y)
        {
            if (y < 0) return true;
            if (x < 0 || x >= WidthCells) return false;
            return y < _height[x];
        }

        public int HeightAt(int x) { return _height[x]; }

        public ulong ContentHash()
        {
            ulong h = WorldHash.Combine(0, WidthCells);
            for (int x = 0; x < _height.Length; x++) h = WorldHash.Combine(h, _height[x]);
            return h;
        }
    }
}
