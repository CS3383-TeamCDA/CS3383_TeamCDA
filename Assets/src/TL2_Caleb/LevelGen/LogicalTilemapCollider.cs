using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TL2_LevelGen
{
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(TilemapCollider2D))]
    public class LogicalTilemapCollider : MonoBehaviour
    {
        [SerializeField] private Tilemap collisionTilemap;

        [Tooltip("A Tile asset with Collider Type set to Grid.")]
        [SerializeField] private Tile solidCollisionTile;

        private TileBase[] tileBuffer;
        private TileBase[] clearBuffer;

        private void Reset()
        {
            collisionTilemap = GetComponent<Tilemap>();
        }

        private void Awake()
        {
            if (collisionTilemap == null)
                collisionTilemap = GetComponent<Tilemap>();
        }

        public void BuildChunk(
            int chunkX,
            int chunkWidth,
            int chunkHeight,
            Func<int, int, int> getTileId,
            int airTileId)
        {
            if (collisionTilemap == null ||
                solidCollisionTile == null)
            {
                Debug.LogError(
                    "Assign the collision Tilemap and collision Tile.",
                    this);

                return;
            }

            EnsureBuffers(chunkWidth * chunkHeight);

            int startX = chunkX * chunkWidth;
            int index = 0;

            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkWidth; x++)
                {
                    bool isSolid =
                        getTileId(startX + x, y) != airTileId;

                    tileBuffer[index++] =
                        isSolid ? solidCollisionTile : null;
                }
            }

            collisionTilemap.SetTilesBlock(
                new BoundsInt(
                    startX, 0, 0,
                    chunkWidth, chunkHeight, 1),
                tileBuffer);
        }

        public void ClearChunk(
            int chunkX,
            int chunkWidth,
            int chunkHeight)
        {
            if (collisionTilemap == null)
                return;

            EnsureBuffers(chunkWidth * chunkHeight);

            collisionTilemap.SetTilesBlock(
                new BoundsInt(
                    chunkX * chunkWidth, 0, 0,
                    chunkWidth, chunkHeight, 1),
                clearBuffer);
        }

        private void EnsureBuffers(int count)
        {
            if (tileBuffer == null || tileBuffer.Length != count)
                tileBuffer = new TileBase[count];

            if (clearBuffer == null || clearBuffer.Length != count)
                clearBuffer = new TileBase[count];
        }
    }
}