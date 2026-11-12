using System;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Calculates an asymmetric, speed-aware chunk window for horizontal gameplay.</summary>
    public sealed class SideScrollerStreamPolicy
    {
        readonly int _behindChunks;
        readonly int _baseAheadChunks;
        readonly int _maxAheadChunks;
        readonly int _verticalRadius;
        readonly float _lookaheadSeconds;

        public SideScrollerStreamPolicy(int behindChunks, int baseAheadChunks, int maxAheadChunks,
            int verticalRadius, float lookaheadSeconds)
        {
            _behindChunks = Math.Max(0, behindChunks);
            _baseAheadChunks = Math.Max(1, baseAheadChunks);
            _maxAheadChunks = Math.Max(_baseAheadChunks, maxAheadChunks);
            _verticalRadius = Math.Max(0, verticalRadius);
            _lookaheadSeconds = float.IsNaN(lookaheadSeconds) || lookaheadSeconds < 0f ? 0f : lookaheadSeconds;
        }

        public WorldStreamWindow GetWindow(WorldPosition2D position, WorldPosition2D velocity)
        {
            int cellX = FloorToCell(position.X);
            int cellY = FloorToCell(position.Y);
            WorldChunkCoord center = WorldChunkCoord.AtCell(cellX, cellY);
            int direction = velocity.X < 0f ? -1 : 1;
            float speed = float.IsNaN(velocity.X) || float.IsInfinity(velocity.X) ? 0f : Math.Abs(velocity.X);
            float requestedCells = _baseAheadChunks * WorldChunkCoord.Size + speed * _lookaheadSeconds;
            int maximumCells = _maxAheadChunks * WorldChunkCoord.Size;
            bool capped = requestedCells > maximumCells;
            float lookaheadCells = capped ? maximumCells : requestedCells;
            int aheadChunks = Math.Max(1, (int)Math.Ceiling(lookaheadCells / WorldChunkCoord.Size));

            int minX = direction > 0 ? center.X - _behindChunks : center.X - aheadChunks;
            int maxX = direction > 0 ? center.X + aheadChunks : center.X + _behindChunks;
            return new WorldStreamWindow(center, minX, maxX,
                center.Y - _verticalRadius, center.Y + _verticalRadius,
                direction, aheadChunks, capped);
        }

        static int FloorToCell(float position)
        {
            if (float.IsNaN(position)) return 0;
            if (position <= int.MinValue + 1f) return int.MinValue;
            if (position >= int.MaxValue - 1f) return int.MaxValue;
            return (int)Math.Floor(position + 0.5f);
        }
    }
}
