using System;

namespace EscapeThe90s.Contracts
{
    /// <summary>Stable public identity for a streamed chunk, independent of generator implementation.</summary>
    public readonly struct ChunkId : IEquatable<ChunkId>
    {
        public readonly ulong StreetId;
        public readonly int Index;

        public ChunkId(ulong streetId, int index) { StreetId = streetId; Index = index; }
        public bool Equals(ChunkId other) { return StreetId == other.StreetId && Index == other.Index; }
        public override bool Equals(object obj) { return obj is ChunkId other && Equals(other); }
        public override int GetHashCode() { unchecked { return (int)(StreetId ^ (StreetId >> 32)) * 397 ^ Index; } }
    }
}
