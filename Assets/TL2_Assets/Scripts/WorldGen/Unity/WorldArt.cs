using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeThe90s.WorldGen
{
    [Serializable]
    public class BuildingArtEntry
    {
        public BuildingType type = BuildingType.Generic;
        [Tooltip("Index-art sprites (one per design variant). Shared by every biome; palette does the colouring.")]
        public Sprite[] variants;
    }

    /// <summary>Everything the renderer needs to draw a street: material, palette, ground sheet, building sprites.</summary>
    [CreateAssetMenu(menuName = "Escape the 90s/World Art", fileName = "WorldArt")]
    public class WorldArt : ScriptableObject
    {
        public Material spriteMaterial;                 // uses shader EscapeThe90s/PaletteSprite, _PaletteTex assigned
        public Texture2D groundSheet;                   // 4x4 dual-grid sheet of 16 px tiles (ground_dualgrid_index.png)
        public BuildingArtEntry[] buildings;

        static readonly int RowId = Shader.PropertyToID("_PaletteRow");
        static MaterialPropertyBlock _mpb;

        [NonSerialized] Tile[] _tiles;
        [NonSerialized] bool _warnedMissing;

        /// <summary>palette.png row for a biome + accent (wraps safely).</summary>
        public static int PaletteRow(int biome, int accent)
        {
            int b = ((biome % WorldConstants.BiomeCount) + WorldConstants.BiomeCount) % WorldConstants.BiomeCount;
            int a = ((accent % WorldConstants.AccentsPerBiome) + WorldConstants.AccentsPerBiome) % WorldConstants.AccentsPerBiome;
            return b * WorldConstants.AccentsPerBiome + a;
        }

        public void ApplyPalette(Renderer r, int row)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            r.sharedMaterial = spriteMaterial;
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(RowId, row);
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Tile for a dual-grid mask 0..15, built at runtime by slicing the sheet (no manual sprite slicing needed).</summary>
        public Tile GetGroundTile(int mask)
        {
            if (_tiles == null || _tiles[mask] == null) BuildTiles();
            return _tiles[mask];
        }

        void BuildTiles()
        {
            _tiles = new Tile[16];
            const int s = WorldConstants.CellPixels;
            for (int mask = 0; mask < 16; mask++)
            {
                int col = DualGrid.SheetColumn(mask), rowFromTop = DualGrid.SheetRowFromTop(mask);
                var rect = new Rect(col * s, groundSheet.height - (rowFromTop + 1) * s, s, s);
                Sprite sp = Sprite.Create(groundSheet, rect, new Vector2(0.5f, 0.5f), s, 0, SpriteMeshType.FullRect);
                sp.name = "ground_" + mask;
                sp.hideFlags = HideFlags.DontSave;
                Tile t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = sp;
                t.colliderType = Tile.ColliderType.None;
                t.hideFlags = HideFlags.DontSave;
                _tiles[mask] = t;
            }
        }

        public Sprite GetBuildingSprite(BuildingType type, int variant)
        {
            Sprite s = Find(type, variant);
            if (s != null) return s;
            if (type != BuildingType.Generic)
            {
                if (!_warnedMissing) { Debug.LogWarning("WorldArt: no sprite for " + type + "; using Generic as a stand-in."); _warnedMissing = true; }
                return Find(BuildingType.Generic, variant);
            }
            return null;
        }

        Sprite Find(BuildingType type, int variant)
        {
            if (buildings == null) return null;
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildings[i].type != type || buildings[i].variants == null || buildings[i].variants.Length == 0) continue;
                return buildings[i].variants[variant % buildings[i].variants.Length];
            }
            return null;
        }
    }
}
