using UnityEngine;
using UnityEngine.Tilemaps;

namespace TL2_LevelGen
{
    [CreateAssetMenu(fileName = "RotatedTile", menuName = "TL2/Rotated Tile")]
    public class RotatedTile : TileBase
    {
        public TileBase baseTile;
        
        [Tooltip("Rotation in degrees (e.g., 90, 180, 270)")]
        public float rotationAngle;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            if (baseTile != null)
            {
                // Pull data from the original tile asset
                baseTile.GetTileData(position, tilemap, ref tileData);
                
                // Inject the rotation matrix onto the existing tile transform
                Quaternion rotation = Quaternion.Euler(0, 0, rotationAngle);
                tileData.transform = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
            }
        }
    }
}
