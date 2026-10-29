using System;

namespace TL2_WorldData
{
    /// <summary>
    /// Streaming 64-bit hash (FNV-1a core + SplitMix finalizer). Fixed algorithm and byte order,
    /// so a chunk gives the same value on every machine, runtime and Unity version.
    /// </summary>
    public struct HashBuilder
    {
        ulong h;
        const ulong Prime = 0x100000001B3UL;

        public static HashBuilder Create()
        {
            var b = new HashBuilder();
            b.h = 0xCBF29CE484222325UL;
            return b;
        }

        public void Add(byte v) { unchecked { h = (h ^ v) * Prime; } }
        public void Add(ushort v) { Add((byte)(v & 0xFF)); Add((byte)(v >> 8)); }
        public void Add(int v) { Add((ushort)(v & 0xFFFF)); Add((ushort)((v >> 16) & 0xFFFF)); }
        public void Add(ulong v)
        {
            Add(unchecked((int)(v & 0xFFFFFFFFUL)));
            Add(unchecked((int)(v >> 32)));
        }

        public ulong Finish()
        {
            unchecked
            {
                ulong z = h;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }

    public static class ChunkHasher
    {
        /// <summary>Hash of one tile layer. Lets a failing test say WHICH layer differs.</summary>
        public static ulong HashLayer(ChunkData c, TileLayer layer)
        {
            var b = HashBuilder.Create();
            b.Add((byte)layer);
            b.Add(c.Width);
            b.Add(c.Height);
            int o = c.LayerOffset(layer);
            for (int i = 0; i < c.LayerSize; i++) b.Add(c.Tiles[o + i]);
            return b.Finish();
        }

        /// <summary>
        /// Hash of the static layer: dimensions, layer count, then every tile layer in enum order.
        /// Ignores chunk coordinates and tier. Must NOT change with progression tier.
        /// </summary>
        public static ulong Static(ChunkData c)
        {
            var b = HashBuilder.Create();
            b.Add(c.Width);
            b.Add(c.Height);
            b.Add(Layers.Count);
            for (int l = 0; l < Layers.Count; l++) b.Add(HashLayer(c, (TileLayer)l));
            return b.Finish();
        }

        /// <summary>Hash of the dynamic layer. Spawn ORDER is part of the hash on purpose.</summary>
        public static ulong Dynamic(ChunkData c)
        {
            var b = HashBuilder.Create();
            b.Add(c.Spawns.Count);
            for (int i = 0; i < c.Spawns.Count; i++)
            {
                SpawnPoint s = c.Spawns[i];
                b.Add(s.TypeId);
                b.Add(s.X);
                b.Add(s.Y);
                b.Add(s.Variant);
            }
            return b.Finish();
        }

        public static ulong Full(ChunkData c)
        {
            var b = HashBuilder.Create();
            b.Add(Static(c));
            b.Add(Dynamic(c));
            return b.Finish();
        }

        /// <summary>Human-readable first difference, for failure messages.</summary>
        public static string Diff(ChunkData a, ChunkData b)
        {
            if (a.Width != b.Width || a.Height != b.Height) return "chunk sizes differ";
            for (int i = 0; i < a.Tiles.Length; i++)
            {
                if (a.Tiles[i] == b.Tiles[i]) continue;
                int layer = i / a.LayerSize;
                int rem = i % a.LayerSize;
                return $"first tile difference on layer {(TileLayer)layer} at local ({rem % a.Width},{rem / a.Width}): {a.Tiles[i]} vs {b.Tiles[i]}";
            }
            if (a.Spawns.Count != b.Spawns.Count) return $"spawn counts differ: {a.Spawns.Count} vs {b.Spawns.Count}";
            for (int i = 0; i < a.Spawns.Count; i++)
            {
                SpawnPoint p = a.Spawns[i], q = b.Spawns[i];
                if (p.TypeId != q.TypeId || p.X != q.X || p.Y != q.Y || p.Variant != q.Variant)
                    return $"spawn #{i} differs";
            }
            return "identical";
        }
    }
}