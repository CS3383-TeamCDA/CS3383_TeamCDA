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
        readonly bool[] _gap;
        public int WidthCells { get; private set; }
        public int MaxHeight { get; private set; }

        public StreetTerrain(ulong seed, StreetLayout layout, bool bumps)
        {
            WidthCells = layout.ChunkCount * WorldConstants.ChunkWidthCells;
            _height = new int[WidthCells];
            _gap = new bool[WidthCells];
            if (!bumps) return;

            int w = WorldConstants.ChunkWidthCells;
            for (int i = 0; i < layout.ChunkCount; i++)
            {
                ChunkData c = layout.Chunks[i];
                if (c.IsSafe || c.Building != BuildingType.None) continue;
                if (layout.ChunkSpecs != null && i < layout.ChunkSpecs.Length)
                {
                    ApplyStamp(i * w, layout.ChunkSpecs[i].Stamp);
                    continue;
                }
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
            if (x < 0 || x >= WidthCells) return y < 0;
            if (_gap[x]) return false;
            return y < _height[x];
        }

        public int HeightAt(int x) { return _height[x]; }
        public bool IsGap(int x) { return x >= 0 && x < WidthCells && _gap[x]; }

        void ApplyStamp(int x0, TerrainStamp stamp)
        {
            if (stamp.Kind == TerrainStampKind.Gap)
            {
                for (int x = x0 + stamp.StartCell; x < x0 + stamp.StartCell + stamp.WidthCells; x++)
                    if (x >= 0 && x < WidthCells) _gap[x] = true;
                return;
            }

            if (stamp.Kind == TerrainStampKind.Flat || stamp.Kind == TerrainStampKind.Obstacle || stamp.Kind == TerrainStampKind.Plaza)
                return;

            int width = stamp.WidthCells;
            for (int offset = 0; offset < width; offset++)
            {
                int x = x0 + stamp.StartCell + offset;
                if (x < 0 || x >= WidthCells) continue;
                int height = stamp.StartHeight + (stamp.EndHeight - stamp.StartHeight) * (offset + 1) / width;
                _height[x] = height;
                if (height > MaxHeight) MaxHeight = height;
            }
        }

        public ulong ContentHash()
        {
            ulong h = WorldHash.Combine(0, WidthCells);
            for (int x = 0; x < _height.Length; x++)
            {
                h = WorldHash.Combine(h, _height[x]);
                h = WorldHash.Combine(h, _gap[x] ? 1 : 0);
            }
            return h;
        }
    }
}
