using UnityEngine;
using UnityEngine.Tilemaps;

namespace TL2_LevelGen
{
    [CreateAssetMenu(
        fileName = "DualGridTileSet",
        menuName = "TL2/ Dual Grid Tile Set")]
    public class DualGridTileSet : ScriptableObject
    {
        [System.Serializable]
        public class MaskTiles
        {
            [Tooltip("0000: all air. Usually leave empty.")]
            public TileBase mask00;

            [Tooltip("0001: bottom-left")]
            public TileBase mask01;

            [Tooltip("0010: bottom-right")]
            public TileBase mask02;

            [Tooltip("0011: bottom-left + bottom-right")]
            public TileBase mask03;

            [Tooltip("0100: top-left")]
            public TileBase mask04;

            [Tooltip("0101: bottom-left + top-left")]
            public TileBase mask05;

            [Tooltip("0110: bottom-right + top-left")]
            public TileBase mask06;

            [Tooltip("0111: all except top-right")]
            public TileBase mask07;

            [Tooltip("1000: top-right")]
            public TileBase mask08;

            [Tooltip("1001: bottom-left + top-right")]
            public TileBase mask09;

            [Tooltip("1010: bottom-right + top-right")]
            public TileBase mask10;

            [Tooltip("1011: all except top-left")]
            public TileBase mask11;

            [Tooltip("1100: top-left + top-right")]
            public TileBase mask12;

            [Tooltip("1101: all except bottom-right")]
            public TileBase mask13;

            [Tooltip("1110: all except bottom-left")]
            public TileBase mask14;

            [Tooltip("1111: all solid")]
            public TileBase mask15;

            public TileBase Get(int mask)
            {
                switch (mask)
                {
                    case 0: return mask00;
                    case 1: return mask01;
                    case 2: return mask02;
                    case 3: return mask03;
                    case 4: return mask04;
                    case 5: return mask05;
                    case 6: return mask06;
                    case 7: return mask07;
                    case 8: return mask08;
                    case 9: return mask09;
                    case 10: return mask10;
                    case 11: return mask11;
                    case 12: return mask12;
                    case 13: return mask13;
                    case 14: return mask14;
                    case 15: return mask15;
                    default: return null;
                }
            }
        }

        [SerializeField] private MaskTiles tiles = new MaskTiles();

        public TileBase GetTile(int mask)
        {
            return tiles.Get(mask);
        }
    }
}