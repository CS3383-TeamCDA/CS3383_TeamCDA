namespace EscapeThe90s.Contracts
{
    /// <summary>
    /// Describes one game-state transition reported through IGameState.
    /// </summary>
    public readonly struct GameStateChange
    {
        public GameStateType Previous { get; }
        public GameStateType Current { get; }

        /// <summary>
        /// True when Playing is entered to begin a run (from the menu, game over, or a restart).
        /// False for every other transition, including resuming from pause.
        /// </summary>
        public bool IsNewRun { get; }

        public GameStateChange(GameStateType previous, GameStateType current, bool isNewRun)
        {
            Previous = previous;
            Current = current;
            IsNewRun = isNewRun;
        }
    }
}
