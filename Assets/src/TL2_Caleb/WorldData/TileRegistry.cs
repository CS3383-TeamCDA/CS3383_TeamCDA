using System;
using System.Collections.Generic;

namespace TL2_WorldData
{
    /// <summary>
    /// Maps a tile ID to its layer and flags. Pure C# (no Unity types) so generation, tests and
    /// worker threads can use it. Lookups are flat-array reads. Immutable once built, so it is
    /// safe to share across threads.
    ///
    /// ID 0 is always AIR: no flags, valid in every layer, draws nothing.
    /// IDs are PERMANENT: never reuse or renumber one. Saves and golden hashes depend on them.
    /// </summary>
    public sealed class TileRegistry
    {
        readonly TileFlags[] flags;
        readonly TileLayer[] layers;
        readonly bool[] defined;
        readonly string[] names;

        /// <summary>Number of defined tiles, including air.</summary>
        public int Count { get; }

        /// <summary>Highest defined ID.</summary>
        public int MaxId => defined.Length - 1;

        internal TileRegistry(TileFlags[] flags, TileLayer[] layers, bool[] defined, string[] names, int count)
        {
            this.flags = flags; this.layers = layers; this.defined = defined; this.names = names;
            Count = count;
        }

        public bool IsDefined(ushort id) => id < defined.Length && defined[id];

        void Require(ushort id)
        {
            if (id >= defined.Length || !defined[id])
                throw new ArgumentException($"Tile id {id} is not defined in the registry", nameof(id));
        }

        public TileFlags GetFlags(ushort id) { Require(id); return flags[id]; }

        /// <summary>Layer the tile belongs to. Air (0) belongs to every layer: use CanPlaceOnLayer for placement checks.</summary>
        public TileLayer GetLayer(ushort id) { Require(id); return layers[id]; }

        public string GetName(ushort id) { Require(id); return names[id]; }

        /// <summary>True if the tile has ALL of the given flags.</summary>
        public bool Has(ushort id, TileFlags f) { Require(id); return (flags[id] & f) == f; }

        /// <summary>Air may go in any layer. Everything else only in its declared layer.</summary>
        public bool CanPlaceOnLayer(ushort id, TileLayer layer)
        {
            Require(id);
            return id == 0 || layers[id] == layer;
        }

        public IEnumerable<ushort> DefinedIds()
        {
            for (int i = 0; i < defined.Length; i++)
                if (defined[i]) yield return (ushort)i;
        }
    }

    /// <summary>Collects tile definitions, validates every rule, then produces an immutable registry.</summary>
    public sealed class TileRegistryBuilder
    {
        sealed class Entry
        {
            public ushort Id;
            public string Name;
            public TileLayer Layer;
            public TileFlags Flags;
        }

        readonly Dictionary<ushort, Entry> entries = new Dictionary<ushort, Entry>();
        readonly HashSet<string> usedNames = new HashSet<string>(StringComparer.Ordinal) { "air" };

        public TileRegistryBuilder Add(ushort id, string name, TileLayer layer, TileFlags flags = TileFlags.None)
        {
            if (id == 0)
                throw new ArgumentException("Tile id 0 is reserved for air", nameof(id));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"Tile {id} needs a name", nameof(name));
            if ((int)layer >= Layers.Count)
                throw new ArgumentOutOfRangeException(nameof(layer), $"Tile '{name}' uses an unknown layer");
            if (entries.ContainsKey(id))
                throw new InvalidOperationException($"Duplicate tile id {id} ('{name}' and '{entries[id].Name}')");
            if (usedNames.Contains(name))
                throw new InvalidOperationException($"Duplicate tile name '{name}'");
            if (layer == TileLayer.Background && (flags & TileFlagMasks.Gameplay) != 0)
                throw new ArgumentException(
                    $"Background tile '{name}' has gameplay flags ({flags & TileFlagMasks.Gameplay}). Gameplay never reads the background layer.",
                    nameof(flags));

            usedNames.Add(name);
            entries.Add(id, new Entry { Id = id, Name = name, Layer = layer, Flags = flags });
            return this;
        }

        public TileRegistry Build()
        {
            int maxId = 0;
            foreach (ushort k in entries.Keys) if (k > maxId) maxId = k;

            var flags = new TileFlags[maxId + 1];
            var layers = new TileLayer[maxId + 1];
            var defined = new bool[maxId + 1];
            var names = new string[maxId + 1];

            defined[0] = true;
            names[0] = "air";
            layers[0] = TileLayer.Foreground;   // placeholder: air is valid in every layer

            foreach (Entry e in entries.Values)
            {
                flags[e.Id] = e.Flags;
                layers[e.Id] = e.Layer;
                defined[e.Id] = true;
                names[e.Id] = e.Name;
            }
            return new TileRegistry(flags, layers, defined, names, entries.Count + 1);
        }
    }
}