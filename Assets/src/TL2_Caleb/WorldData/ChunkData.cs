using System;
using System.Collections.Generic;

namespace TL2_WorldData
{
    /// <summary>
    /// Pure data for one chunk. No Unity types.
    /// STATIC layer: Tiles (all tile layers). Depends only on seed + coordinates.
    /// DYNAMIC layer: Spawns. Also depends on progression tier.
    /// Tile layout: one contiguous array, index = layer * LayerSize + y * Width + x, y points up.
    /// </summary>
    public sealed class ChunkData
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int LayerSize;

        // Identity. Set by Reset(). NOT part of the hash (identity is not content).
        public int ChunkX;
        public int ChunkY;
        public int Tier;          // tier the dynamic layer was built for

        /// <summary>Raw tile storage for hashing and bulk copies. Prefer Get/Set elsewhere.</summary>
        public readonly ushort[] Tiles;

        /// <summary>Planned spawns. Order is stable and part of the dynamic hash.</summary>
        public readonly List<SpawnPoint> Spawns = new List<SpawnPoint>();

        public ChunkData(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            LayerSize = width * height;
            Tiles = new ushort[Layers.Count * LayerSize];
        }

        public int LayerOffset(TileLayer layer)
        {
            CheckLayer(layer);
            return (int)layer * LayerSize;
        }

        public int Index(TileLayer layer, int x, int y)
        {
            CheckLayer(layer);
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) ThrowOutOfBounds(x, y);
            return (int)layer * LayerSize + y * Width + x;
        }

        public ushort Get(TileLayer layer, int x, int y) => Tiles[Index(layer, x, y)];

        public void Set(TileLayer layer, int x, int y, ushort id) => Tiles[Index(layer, x, y)] = id;

        public void Fill(TileLayer layer, ushort id)
        {
            int o = LayerOffset(layer);
            for (int i = 0; i < LayerSize; i++) Tiles[o + i] = id;
        }

        /// <summary>Clears every tile and spawn and sets identity. Lets chunks be pooled and reused.</summary>
        public void Reset(int chunkX, int chunkY, int tier)
        {
            Array.Clear(Tiles, 0, Tiles.Length);
            Spawns.Clear();
            ChunkX = chunkX;
            ChunkY = chunkY;
            Tier = tier;
        }

        static void CheckLayer(TileLayer layer)
        {
            if ((int)layer >= Layers.Count)
                throw new ArgumentOutOfRangeException(nameof(layer), $"Unknown layer {(int)layer}");
        }

        void ThrowOutOfBounds(int x, int y)
        {
            throw new ArgumentOutOfRangeException(
                $"({x},{y})", $"Tile coordinate outside chunk of size {Width}x{Height}");
        }
    }
}