namespace EscapeThe90s.Contracts
{
    /// <summary>One defeat, with independent score and level-scaled currency rewards.</summary>
    public readonly struct EnemyDefeatedInfo
    {
        public EnemyType Type { get; }
        public int Level { get; }
        public int ScoreValue { get; }
        public int CoinReward { get; }
        public WorldPosition2D Position { get; }

        public EnemyDefeatedInfo(EnemyType type, int level, int scoreValue, int coinReward, WorldPosition2D position)
        {
            Type = type;
            Level = level;
            ScoreValue = scoreValue;
            CoinReward = coinReward;
            Position = position;
        }
    }
}
