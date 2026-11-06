using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Converts the player boundary into a hysteretic chunk window for StreetView.</summary>
    public sealed class LevelStreamer
    {
        public ChunkStreamWindow GetWindow(Transform streetRoot, IPlayerPositionSource positionSource,
            int chunkCount, int radius)
        {
            if (streetRoot == null || positionSource == null || chunkCount <= 0)
                return new ChunkStreamWindow(0, 0, -1, 0f);
            WorldPosition2D position = positionSource.GetPosition();
            float localX = streetRoot.InverseTransformPoint(new Vector3(position.X, position.Y, 0f)).x;
            int center = ChunkWindow.ChunkIndexAt(localX, chunkCount);
            int first = Mathf.Max(0, center - radius);
            int last = Mathf.Min(chunkCount - 1, center + radius);
            return new ChunkStreamWindow(center, first, last, localX);
        }

        public bool ShouldRecycle(int chunkIndex, int center, int radius)
        { return ChunkWindow.ShouldUnload(chunkIndex, center, radius); }
    }
}
