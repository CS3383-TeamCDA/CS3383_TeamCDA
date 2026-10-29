using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TL2_LevelGen
{
    [RequireComponent(typeof(Tilemap))]
    public class DualGridTilemapRenderer : LevelChunkRenderer
    {
        [SerializeField] private Tilemap targetTilemap;
        [SerializeField] private DualGridTileSet tileSet;

        private TileBase[] renderBuffer;
        private TileBase[] clearBuffer;

        private void Reset()
        {
            targetTilemap = GetComponent<Tilemap>();
        }

        private void Awake()
        {
            if (targetTilemap == null)
                targetTilemap = GetComponent<Tilemap>();
        }

        public override void RenderChunk(
            int chunkX,
            int chunkWidth,
            int chunkHeight,
            Func<int, int, int> getTileId,
            int airTileId)
        {
            if (targetTilemap == null || tileSet == null)
            {
                Debug.LogError(
                    "Assign a Tilemap and Dual Grid Tile Set.",
                    this);

                return;
            }

            // Include y = -1 to draw the bottom terrain edge.
            int renderHeight = chunkHeight + 1;
            int count = chunkWidth * renderHeight;

            EnsureBuffers(count);

            int startX = chunkX * chunkWidth;
            int index = 0;

            for (int y = -1; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkWidth; x++)
                {
                    int worldX = startX + x;
                    int mask = 0;

                    if (getTileId(worldX, y) != airTileId)
                        mask |= 1; // Bottom-left

                    if (getTileId(worldX + 1, y) != airTileId)
                        mask |= 2; // Bottom-right

                    if (getTileId(worldX, y + 1) != airTileId)
                        mask |= 4; // Top-left

                    if (getTileId(worldX + 1, y + 1) != airTileId)
                        mask |= 8; // Top-right

                    renderBuffer[index++] = tileSet.GetTile(mask);
                }
            }

            BoundsInt bounds = new BoundsInt(
                startX,
                -1,
                0,
                chunkWidth,
                renderHeight,
                1);

            targetTilemap.SetTilesBlock(bounds, renderBuffer);

            // Only chunk zero owns the world's left outside edge.
            if (chunkX == 0)
                RenderLeftBoundary(chunkHeight, getTileId, airTileId);
        }

        public override void ClearChunk(
            int chunkX,
            int chunkWidth,
            int chunkHeight)
        {
            if (targetTilemap == null)
                return;

            int renderHeight = chunkHeight + 1;
            EnsureBuffers(chunkWidth * renderHeight);

            BoundsInt bounds = new BoundsInt(
                chunkX * chunkWidth,
                -1,
                0,
                chunkWidth,
                renderHeight,
                1);

            targetTilemap.SetTilesBlock(bounds, clearBuffer);

            if (chunkX == 0)
            {
                for (int y = -1; y < chunkHeight; y++)
                {
                    targetTilemap.SetTile(
                        new Vector3Int(-1, y, 0), null);
                }
            }
        }

        private void RenderLeftBoundary(
            int chunkHeight,
            Func<int, int, int> getTileId,
            int airTileId)
        {
            for (int y = -1; y < chunkHeight; y++)
            {
                int mask = 0;

                // x = -1 is outside the world, so only the
                // right-hand corners can be solid.
                if (getTileId(0, y) != airTileId)
                    mask |= 2;

                if (getTileId(0, y + 1) != airTileId)
                    mask |= 8;

                targetTilemap.SetTile(
                    new Vector3Int(-1, y, 0),
                    tileSet.GetTile(mask));
            }
        }

        private void EnsureBuffers(int count)
        {
            if (renderBuffer == null || renderBuffer.Length != count)
                renderBuffer = new TileBase[count];

            if (clearBuffer == null || clearBuffer.Length != count)
                clearBuffer = new TileBase[count];
        }
    }
}