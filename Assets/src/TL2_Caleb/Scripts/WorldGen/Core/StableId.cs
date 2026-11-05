using System;

namespace EscapeThe90s.WorldGen
{
    public readonly struct Int2 : IEquatable<Int2>, IComparable<Int2>
    {
        public readonly int X, Y;
        public Int2(int x, int y) { X = x; Y = y; }
        public bool Equals(Int2 o) { return X == o.X && Y == o.Y; }
        public override bool Equals(object o) { return o is Int2 i && Equals(i); }
        public override int GetHashCode() { unchecked { return X * 73856093 ^ Y * 19349663; } }
        public int CompareTo(Int2 o) { int c = X.CompareTo(o.X); return c != 0 ? c : Y.CompareTo(o.Y); }
        public override string ToString() { return "(" + X + "," + Y + ")"; }
    }

    /// <summary>
    /// A seed-independent identity for a thing in the world (street, chunk, tower pair...).
    /// Derived only from structural coordinates, so every system computes the same ID.
    /// </summary>
    public readonly struct StableId : IEquatable<StableId>
    {
        public readonly ulong Value;
        public StableId(ulong value) { Value = value; }

        const int TagStreet = 0x5701, TagChild = 0x5702, TagPair = 0x5703, TagRegion = 0x5704, TagRegionPair = 0x5705;

        /// <summary>Street between two graph nodes. Endpoint order does not matter.</summary>
        public static StableId ForStreet(Int2 a, Int2 b)
        {
            if (a.CompareTo(b) > 0) { Int2 t = a; a = b; b = t; }
            ulong h = WorldHash.Combine(0, TagStreet);
            h = WorldHash.Combine(h, a.X); h = WorldHash.Combine(h, a.Y);
            h = WorldHash.Combine(h, b.X); h = WorldHash.Combine(h, b.Y);
            return new StableId(h);
        }

        public static StableId ForRegion(Int2 cell)
        {
            ulong h = WorldHash.Combine(0, TagRegion);
            h = WorldHash.Combine(h, cell.X);
            return new StableId(WorldHash.Combine(h, cell.Y));
        }

        /// <summary>Two adjacent regions. Order does not matter.</summary>
        public static StableId ForRegionPair(Int2 a, Int2 b)
        {
            if (a.CompareTo(b) > 0) { Int2 t = a; a = b; b = t; }
            ulong h = WorldHash.Combine(0, TagRegionPair);
            h = WorldHash.Combine(h, a.X); h = WorldHash.Combine(h, a.Y);
            h = WorldHash.Combine(h, b.X);
            return new StableId(WorldHash.Combine(h, b.Y));
        }

        /// <summary>A sub-entity: chunk, enemy slot, etc.</summary>
        public StableId Child(int kind, int index)
        {
            ulong h = WorldHash.Combine(Value, TagChild);
            h = WorldHash.Combine(h, kind);
            return new StableId(WorldHash.Combine(h, index));
        }

        /// <summary>A symmetric pair (e.g. two towers). Order does not matter.</summary>
        public static StableId ForPair(StableId a, StableId b)
        {
            ulong lo = a.Value < b.Value ? a.Value : b.Value;
            ulong hi = a.Value < b.Value ? b.Value : a.Value;
            ulong h = WorldHash.Combine(0, TagPair);
            return new StableId(WorldHash.Combine(WorldHash.Combine(h, lo), hi));
        }

        public bool Equals(StableId o) { return Value == o.Value; }
        public override bool Equals(object o) { return o is StableId s && Equals(s); }
        public override int GetHashCode() { return (int)(Value ^ (Value >> 32)); }
        public override string ToString() { return Value.ToString("X16"); }
    }
}
