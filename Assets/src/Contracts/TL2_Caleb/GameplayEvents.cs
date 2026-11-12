namespace EscapeThe90s.Contracts
{
    public readonly struct GameStartedEvent { }
    public readonly struct MenuEnteredEvent { }
    public readonly struct GamePausedEvent { }
    public readonly struct GameResumedEvent { }
    public readonly struct GameOverEvent { }
    public readonly struct PlayerDiedEvent { }

    /// <summary>Small query contract used by systems that must wait for active gameplay.</summary>
    public interface IGameplaySessionState
    {
        bool IsGameplayStarted { get; }
    }
}
