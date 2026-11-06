namespace EscapeThe90s.Contracts
{
    /// <summary>Immutable normalized spawn instruction published with a generated chunk.</summary>
    public readonly struct SpawnLocation
    {
        public readonly SpawnLocationKind Kind;
        public readonly float NormalizedX;
        public readonly int SlotIndex;
        public readonly int ContentKindIndex;
        public readonly int AnchorIndex;

        public SpawnLocation(SpawnLocationKind kind, float normalizedX, int slotIndex,
            int contentKindIndex, int anchorIndex)
        {
            Kind = kind;
            NormalizedX = Clamp01(normalizedX);
            SlotIndex = slotIndex;
            ContentKindIndex = contentKindIndex;
            AnchorIndex = anchorIndex;
        }

        static float Clamp01(float value)
        {
            if (float.IsNaN(value) || value <= 0f) return 0f;
            return value >= 1f ? 1f : value;
        }
    }
}
