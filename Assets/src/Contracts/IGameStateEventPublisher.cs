using System;

namespace EscapeThe90s.Contracts
{
    /// <summary>Compatibility contract for existing consumers; new code should use IEventBus.</summary>
    [Obsolete("Use IEventBus with typed lifecycle events.")]
    public interface IGameStateEventPublisher
    {
        event Action OnGameStarted;
        event Action OnMenuEntered;
        event Action OnPlayerDied;
        void NotifyPlayerDied();
    }
}
