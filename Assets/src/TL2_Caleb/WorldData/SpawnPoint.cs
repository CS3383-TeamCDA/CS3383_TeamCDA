namespace TL2_WorldData
{
    /// <summary>
    /// One planned mob placement. Part of the DYNAMIC layer (depends on progression tier).
    /// Integers only: if sub-tile positions are needed, use fixed-point, never floats.
    /// </summary>
    public struct SpawnPoint
    {
        public ushort TypeId;
        public int X;          // chunk-local tile X
        public int Y;          // chunk-local tile Y of the LOWEST tile the mob occupies
        public byte Variant;   // elite / rarity tier etc.

        public SpawnPoint(ushort typeId, int x, int y, byte variant = 0)
        {
            TypeId = typeId; X = x; Y = y; Variant = variant;
        }
    }
}