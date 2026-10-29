using System.Collections.Generic;
using TL2_WorldData;
using UnityEngine;

namespace TL2_Tiles
{
    /// <summary>Lists every TileDef and builds the pure TileRegistry from them once.</summary>
    [CreateAssetMenu(menuName = "TL2/Tile Registry", fileName = "TileRegistry")]
    public sealed class TileRegistryAsset : ScriptableObject
    {
        [SerializeField] List<TileDef> tiles = new List<TileDef>();

        TileRegistry cached;

        void OnEnable() { cached = null; }

        public IReadOnlyList<TileDef> Tiles => tiles;

        /// <summary>Built on first use and cached. Throws with a clear message if any rule is broken.</summary>
        public TileRegistry Registry
        {
            get
            {
                if (cached == null) cached = BuildRegistry();
                return cached;
            }
        }

        public TileRegistry BuildRegistry()
        {
            var builder = new TileRegistryBuilder();
            foreach (TileDef def in tiles)
            {
                if (def == null)
                    throw new System.InvalidOperationException($"TileRegistryAsset '{name}' has an empty slot");
                if (def.Id < 1 || def.Id > ushort.MaxValue)
                    throw new System.InvalidOperationException($"TileDef '{def.name}' has out-of-range id {def.Id}");
                builder.Add((ushort)def.Id, def.DisplayName, def.Layer, def.Flags);
            }
            return builder.Build();
        }
    }
}