using TL2_WorldData;
using UnityEngine;

namespace TL2_Tiles
{
    /// <summary>
    /// Authoring asset for one tile type (gameplay data only, no art). IDs are PERMANENT: assign explicitly, never reuse or renumber.
    /// Note: Unity serializes enums as ints, so keep TileFlags bits below 31 to stay inspector-safe.
    /// </summary>
    [CreateAssetMenu(menuName = "TL2/Tile Def", fileName = "Tile_")]
    public sealed class TileDef : ScriptableObject
    {
        [SerializeField] int id;
        [SerializeField] string displayName;
        [SerializeField] TileLayer layer = TileLayer.Foreground;
        [SerializeField] TileFlags flags;
        // Gameplay data only. Visuals (dual-grid tilesets, sprites) live in a separate renderer-side asset.
        // Later: hardness, drop table, sounds ...

        public int Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public TileLayer Layer => layer;
        public TileFlags Flags => flags;

        void OnValidate()
        {
            if (id < 1 || id > ushort.MaxValue)
                Debug.LogWarning($"TileDef '{name}': id must be between 1 and {ushort.MaxValue} (0 is air).", this);
        }
    }
}