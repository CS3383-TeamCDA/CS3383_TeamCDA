namespace EscapeThe90s.Contracts
{
    /// <summary>One power-up pickup by the player.</summary>
    public readonly struct PowerUpCollectedInfo
    {
        public PowerUpType Type { get; }
        public WorldPosition2D Position { get; }
        public int StackCount { get; }          // count after this pickup
        public float RemainingSeconds { get; }  // 0 when IsPermanent
        public bool IsPermanent { get; }

        public PowerUpCollectedInfo(PowerUpType type, WorldPosition2D position, int stackCount,
                                    float remainingSeconds, bool isPermanent)
        {
            Type = type;
            Position = position;
            StackCount = stackCount;
            RemainingSeconds = remainingSeconds;
            IsPermanent = isPermanent;
        }
    }
}
