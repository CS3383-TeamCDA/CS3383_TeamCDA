using System;

namespace EscapeThe90s.Contracts
{
    public interface IBossEncounter
    {
        bool IsActive { get; }
        bool TryStart(BossEncounterRequest request);
        void Cancel();
        event Action<BossEncounterResult> OnEncounterEnded;
    }

    public readonly struct BossEncounterRequest
    {
        public ulong EncounterId { get; }
        public WorldPosition2D Position { get; }
        public int Level { get; }
        public int Seed { get; }

        public BossEncounterRequest(ulong encounterId, WorldPosition2D position, int level, int seed)
        {
            EncounterId = encounterId;
            Position = position;
            Level = level;
            Seed = seed;
        }
    }

    public enum BossEncounterOutcome { Defeated, Cancelled, Despawned }

    public readonly struct BossEncounterResult
    {
        public ulong EncounterId { get; }
        public BossEncounterOutcome Outcome { get; }
        public int ScoreValue { get; }
        public int CoinReward { get; }

        public BossEncounterResult(ulong encounterId, BossEncounterOutcome outcome, int scoreValue, int coinReward)
        {
            EncounterId = encounterId;
            Outcome = outcome;
            ScoreValue = scoreValue;
            CoinReward = coinReward;
        }
    }
}
