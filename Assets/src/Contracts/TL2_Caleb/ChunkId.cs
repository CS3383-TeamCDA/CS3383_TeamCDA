using System;

namespace EscapeThe90s.Contracts
{
    /// <summary>Stable public identity for a streamed chunk, independent of generator implementation.</summary>
    public readonly struct ChunkId : IEquatable<ChunkId>
    {
        public readonly ulong StreetId;
        public readonly int Index;
        public readonly bool IsWorldCoordinate;
        public readonly int WorldX;
        public readonly int WorldY;

        public ChunkId(ulong streetId, int index)
        { StreetId = streetId; Index = index; IsWorldCoordinate = false; WorldX = 0; WorldY = 0; }

        ChunkId(int worldX, int worldY, bool isWorldCoordinate)
        { StreetId = 0UL; Index = 0; IsWorldCoordinate = isWorldCoordinate; WorldX = worldX; WorldY = worldY; }

        /// <summary>Creates a chunk identity that is independent of the street graph.</summary>
        public static ChunkId ForWorldCoordinate(int worldX, int worldY)
        { return new ChunkId(worldX, worldY, true); }

        public bool Equals(ChunkId other)
        {
            if (IsWorldCoordinate != other.IsWorldCoordinate) return false;
            return IsWorldCoordinate
                ? WorldX == other.WorldX && WorldY == other.WorldY
                : StreetId == other.StreetId && Index == other.Index;
        }

        public override bool Equals(object obj) { return obj is ChunkId other && Equals(other); }
        public override int GetHashCode()
        {
            unchecked
            {
                return IsWorldCoordinate ? ((WorldX * 397) ^ WorldY ^ 0x574F524C)
                    : ((int)(StreetId ^ (StreetId >> 32)) * 397 ^ Index);
            }
        }
    }
}
