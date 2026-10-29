using System;
using UnityEngine;

namespace TL2_LevelGen
{
    public abstract class LevelChunkRenderer : MonoBehaviour
    {
        public abstract void RenderChunk(
            int chunkX,
            int chunkWidth,
            int chunkHeight,
            Func<int, int, int> getTileId,
            int airTileId);

        public abstract void ClearChunk(
            int chunkX,
            int chunkWidth,
            int chunkHeight);
    }
}