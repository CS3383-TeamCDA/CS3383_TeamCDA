using System;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Stable address of a 16 by 16 tile chunk in an unbounded world grid.</summary>
    public readonly struct WorldChunkCoord : IEquatable<WorldChunkCoord>, IComparable<WorldChunkCoord>
    {
        public const int Size = WorldConstants.WorldChunkSizeCells;
        public readonly int X;
        public readonly int Y;
        public int MinCellX { get { return X * Size; } }
        public int MinCellY { get { return Y * Size; } }
        public int MaxCellX { get { return MinCellX + Size - 1; } }
        public int MaxCellY { get { return MinCellY + Size - 1; } }

        public WorldChunkCoord(int x, int y) { X = x; Y = y; }
        public int CompareTo(WorldChunkCoord other) { int y = Y.CompareTo(other.Y); return y != 0 ? y : X.CompareTo(other.X); }
        public bool Equals(WorldChunkCoord other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is WorldChunkCoord && Equals((WorldChunkCoord)obj); }
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() { return "(" + X + ", " + Y + ")"; }
        public static bool operator ==(WorldChunkCoord a, WorldChunkCoord b) { return a.Equals(b); }
        public static bool operator !=(WorldChunkCoord a, WorldChunkCoord b) { return !a.Equals(b); }

        public static WorldChunkCoord AtCell(int x, int y)
        { return new WorldChunkCoord(FloorDiv(x, Size), FloorDiv(y, Size)); }

        public WorldChunkCoord Offset(int x, int y) { return new WorldChunkCoord(X + x, Y + y); }

        /// <summary>
        /// Returns the edge neighbors in north, east, south, west order. World Y increases northward.
        /// </summary>
        public WorldChunkCoord[] GetEdgeNeighbors()
        {
            return new[]
            {
                Offset(0, 1),
                Offset(1, 0),
                Offset(0, -1),
                Offset(-1, 0)
            };
        }

        /// <summary>
        /// Returns all neighbors clockwise from north: north, northeast, east, southeast,
        /// south, southwest, west, northwest. World Y increases northward.
        /// </summary>
        public WorldChunkCoord[] GetNeighbors()
        {
            return new[]
            {
                Offset(0, 1),
                Offset(1, 1),
                Offset(1, 0),
                Offset(1, -1),
                Offset(0, -1),
                Offset(-1, -1),
                Offset(-1, 0),
                Offset(-1, 1)
            };
        }

        public bool ContainsCell(int x, int y)
        { return x >= MinCellX && x <= MaxCellX && y >= MinCellY && y <= MaxCellY; }
        public int LocalIndexX(int cellX) { return cellX - MinCellX; }
        public int LocalIndexY(int cellY) { return cellY - MinCellY; }

        public static int FloorDiv(int value, int divisor)
        {
            if (divisor <= 0) throw new ArgumentOutOfRangeException("divisor");
            int quotient = value / divisor;
            return value < 0 && value % divisor != 0 ? quotient - 1 : quotient;
        }
    }
}
