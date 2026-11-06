using System;

namespace EscapeThe90s.Contracts
{
    /// <summary>Engine-independent game lifecycle notifications consumed by gameplay systems.</summary>
    public interface IGameStateEventPublisher
    {
        event Action OnGameStarted;
        event Action OnMenuEntered;
        event Action OnPlayerDied;

        void NotifyPlayerDied();
    }
}
