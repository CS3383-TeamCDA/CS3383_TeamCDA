namespace EscapeThe90s.WorldGen
{
    /// <summary>A boolean terrain you can ask "is this data cell solid?". Y is up; the street surface is y = 0.</summary>
    public interface ICellSource
    {
        bool IsFilled(int x, int y);
    }

    /// <summary>
    /// Dual-grid maths. Render tile (rx, ry) is centred on the corner shared by four data cells:
    ///   TL = (rx-1, ry)   TR = (rx, ry)   BL = (rx-1, ry-1)   BR = (rx, ry-1)
    /// Mask bits match the tile sheet: TL=1, TR=2, BL=4, BR=8. Sheet position of a mask:
    /// column = mask % 4, row = mask / 4 counted from the TOP of the image (see generate_sprites.py).
    /// </summary>
    public static class DualGrid
    {
        public const int TL = 1, TR = 2, BL = 4, BR = 8;

        public static int MaskAt(ICellSource s, int rx, int ry)
        {
            int m = 0;
            if (s.IsFilled(rx - 1, ry)) m |= TL;
            if (s.IsFilled(rx, ry)) m |= TR;
            if (s.IsFilled(rx - 1, ry - 1)) m |= BL;
            if (s.IsFilled(rx, ry - 1)) m |= BR;
            return m;
        }

        public static int SheetColumn(int mask) { return mask & 3; }
        public static int SheetRowFromTop(int mask) { return mask >> 2; }
    }
}
