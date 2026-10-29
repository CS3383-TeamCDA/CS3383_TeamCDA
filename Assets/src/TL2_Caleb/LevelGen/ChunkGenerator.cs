using System;
using UnityEngine;
using TL2_WorldData;
using TL2_Tiles;

namespace TL2_LevelGen
{
    /// <summary>
    /// Deterministically fills ChunkData with primary terrain tile types (Stone, Dirt, Grass, Air)
    /// and enforces a left boundary wall at globalX == 0.
    /// </summary>
    public class ChunkGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private ulong worldSeed = 1337;
        [SerializeField] private int baseSurfaceHeight = 16;
        [SerializeField] private int dirtDepth = 4;
        [SerializeField] private float heightNoiseScale = 0.05f;
        [SerializeField] private int heightVariance = 6;

        [Header("World Bounds")]
        [Tooltip("Absolute grid Y below which everything is solid stone.")]
        [SerializeField] private int worldBottomY = 2;
        [Tooltip("Absolute grid Y above which everything is completely air.")]
        [SerializeField] private int worldTopY = 28;

        [Header("Tile IDs (Matching TileRegistry)")]
        [SerializeField] private byte airTileId = 0;
        [SerializeField] private byte stoneTileId = 1;
        [SerializeField] private byte dirtTileId = 2;
        [SerializeField] private byte grassTileId = 3;

        /// <summary>
        /// Populates a chunk deterministically based on its explicit chunk index.
        /// </summary>
        public void GenerateChunk(ChunkData chunkData, int chunkX)
        {
            int chunkWidth = chunkData.Width;
            int chunkHeight = chunkData.Height;

            // FIX: Combine world seed and the explicit chunk location passed down by the LevelGenerator
            // If you implement a vertical chunking grid later, swap out the hardcoded 0 for chunkY.
            ulong chunkSeed = Hash.Combine(worldSeed, chunkX, 0, 0x1);
            Rng rng = new Rng(chunkSeed);

            for (int x = 0; x < chunkWidth; x++)
            {
                int globalX = (chunkX * chunkWidth) + x;

                // 1. Left Wall Enforcement at global X == 0
                if (globalX <= 0)
                {
                    for (int y = 0; y < chunkHeight; y++)
                    {
                        chunkData.Set(TileLayer.Foreground, x, y, stoneTileId);
                    }
                    continue;
                }

                // 2. Deterministic Heightmap Calculation
                int surfaceY = GetDeterministicHeight(globalX, rng);
                
                // Clamp the calculated surface height so it behaves inside your bounds window safely
                surfaceY = Mathf.Clamp(surfaceY, worldBottomY, worldTopY - 1);

                // 3. Fill Column Data (Air, Grass, Dirt, Stone)
                for (int y = 0; y < chunkHeight; y++)
                {
                    // RULE A: Absolute top ceiling enforcement
                    if (y >= worldTopY)
                    {
                        chunkData.Set(TileLayer.Foreground, x, y, airTileId);
                    }
                    // RULE B: Absolute bottom floor enforcement
                    else if (y <= worldBottomY)
                    {
                        chunkData.Set(TileLayer.Foreground, x, y, stoneTileId);
                    }
                    // Standard Natural Stratification Logic
                    else if (y > surfaceY)
                    {
                        chunkData.Set(TileLayer.Foreground, x, y, airTileId);
                    }
                    else if (y == surfaceY)
                    {
                        chunkData.Set(TileLayer.Foreground, x, y, grassTileId);
                    }
                    else if (y > surfaceY - dirtDepth)
                    {
                        chunkData.Set(TileLayer.Foreground, x, y, dirtTileId);
                    }
                    else
                    {
                        chunkData.Set(TileLayer.Foreground, x, y, stoneTileId);
                    }
                }
            }
        }

        private int GetDeterministicHeight(int globalX, Rng rng)
        {
            // Simple deterministic height curve offset using Perlin noise + Rng jitter
            // We use globalX + a hash offset to ensure Perlin inputs move smoothly down the spatial grid
            float rawNoise = Mathf.PerlinNoise((globalX + (float)(worldSeed % 50000)) * heightNoiseScale, 0.5f);
            
            int jitter = rng.Range(-1, 2); 
            return baseSurfaceHeight + Mathf.RoundToInt(rawNoise * heightVariance) + jitter;
        }

        public byte AirTileId => airTileId;
        public byte StoneTileId => stoneTileId;
        public byte DirtTileId => dirtTileId;
        public byte GrassTileId => grassTileId;
    }
}
