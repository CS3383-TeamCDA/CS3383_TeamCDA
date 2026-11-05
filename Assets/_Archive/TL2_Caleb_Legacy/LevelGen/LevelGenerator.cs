using System.Collections.Generic;
using UnityEngine;
using TL2_WorldData;

namespace TL2_LevelGen
{
    public class LevelGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private ChunkGenerator generator;
        [SerializeField] private LevelChunkRenderer rendererPass;
        [SerializeField] private LogicalTilemapCollider collisionPass;

        [Header("Chunk Configuration")]
        [SerializeField, Min(1)] private int chunkWidth = 16;
        [SerializeField, Min(1)] private int chunkHeight = 32;
        [SerializeField, Min(0)] private int viewDistance = 3;
        [SerializeField, Min(0)] private int chunksBehind = 2;

        private readonly Dictionary<int, ChunkData> loadedChunks =
            new Dictionary<int, ChunkData>();

        private readonly HashSet<int> renderedChunks =
            new HashSet<int>();

        private readonly List<int> removalBuffer =
            new List<int>();

        private int lastPlayerChunkX = int.MinValue;

        private void Start()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            RefreshChunks();
        }

        private void Update()
        {
            if (playerTransform == null)
                return;

            int currentChunk = GetPlayerChunk();

            if (currentChunk != lastPlayerChunkX)
                RefreshChunks();
        }

        private bool ValidateReferences()
        {
            if (playerTransform != null &&
                generator != null &&
                rendererPass != null &&
                collisionPass != null)
            {
                return true;
            }
            return false;
        }

        private int GetPlayerChunk()
        {
            // FloorToInt handles both positive and negative values correctly
            return Mathf.FloorToInt(playerTransform.position.x / chunkWidth);
        }

        public void RefreshChunks()
        {
            if (!ValidateReferences())
                return;

            int currentChunk = GetPlayerChunk();
            lastPlayerChunkX = currentChunk;

            int firstVisible = currentChunk - chunksBehind;
            int lastVisible = currentChunk + viewDistance;

            // The dual-grid renderer below reads x + 1.
            int lastDataChunk = lastVisible + 1;

            // 1. Clear visuals that have left the visible interval.
            removalBuffer.Clear();
            foreach (int chunkX in renderedChunks)
            {
                if (chunkX < firstVisible || chunkX > lastVisible)
                    removalBuffer.Add(chunkX);
            }

            foreach (int chunkX in removalBuffer)
            {
                rendererPass.ClearChunk(
                    chunkX, chunkWidth, chunkHeight);

                collisionPass.ClearChunk(
                    chunkX, chunkWidth, chunkHeight);

                renderedChunks.Remove(chunkX);
            }

            // 2. Remove data outside the required interval.
            removalBuffer.Clear();
            foreach (int chunkX in loadedChunks.Keys)
            {
                if (chunkX < firstVisible || chunkX > lastDataChunk)
                    removalBuffer.Add(chunkX);
            }

            foreach (int chunkX in removalBuffer)
                loadedChunks.Remove(chunkX);

            // 3. Generate all required data before rendering.
            for (int chunkX = firstVisible; chunkX <= lastDataChunk; chunkX++)
            {
                if (loadedChunks.ContainsKey(chunkX))
                    continue;

                ChunkData chunk = new ChunkData(chunkWidth, chunkHeight);

                // FIX: Pass chunkX to your generator so it can offset noise calculations 
                // and break the identical chunk pattern.
                generator.GenerateChunk(chunk, chunkX); 
                
                loadedChunks.Add(chunkX, chunk);
            }

            // 4. Render the visible interval.
            for (int chunkX = firstVisible;
                chunkX <= lastVisible;
                chunkX++)
            {
                rendererPass.RenderChunk(
                    chunkX,
                    chunkWidth,
                    chunkHeight,
                    GetTileId,
                    generator.AirTileId);

                collisionPass.BuildChunk(
                    chunkX,
                    chunkWidth,
                    chunkHeight,
                    GetTileId,
                    generator.AirTileId);

                renderedChunks.Add(chunkX);
            }
        }

        private int GetTileId(int worldX, int worldY)
        {
            // Vertical constraints stay locked to the absolute height bound of a single chunk row
            if (worldY < 0 || worldY >= chunkHeight)
            {
                return generator.AirTileId;
            }

            // FIX: Math.FloorToInt alongside proper mathematical modulo ensures 
            // coordinate fetching works flawlessly across both positive and negative spaces
            int chunkX = Mathf.FloorToInt((float)worldX / chunkWidth);
            int localX = worldX - (chunkX * chunkWidth);

            if (!loadedChunks.TryGetValue(chunkX, out ChunkData chunk))
            {
                return generator.AirTileId;
            }

            return ReadLocalTile(chunk, localX, worldY);
        }

        private static int ReadLocalTile(ChunkData chunk, int x, int y)
        {
            return chunk.Get(TileLayer.Foreground, x, y);
        }

        private void OnValidate()
        {
            chunkWidth = Mathf.Max(1, chunkWidth);
            chunkHeight = Mathf.Max(1, chunkHeight);
            viewDistance = Mathf.Max(0, viewDistance);
            chunksBehind = Mathf.Max(0, chunksBehind);
        }
    }
}
