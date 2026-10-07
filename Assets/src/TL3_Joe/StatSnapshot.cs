namespace EscapeThe90s.PowerUps
{
    /// <summary>Which player stat a snapshot holds.</summary>
    public enum StatKind
    {
        SpeedMultiplier,
        PickupRadius
    }

    /// <summary>
    /// Memento: one saved stat and its value. Immutable once created.
    /// PowerUpManager (the Caretaker) stores these but never reads inside them.
    /// </summary>
    public sealed class StatSnapshot
    {
        public StatKind Stat { get; }
        public float Value { get; }

        public StatSnapshot(StatKind stat, float value)
        {
            Stat = stat;
            Value = value;
        }
    }
}
