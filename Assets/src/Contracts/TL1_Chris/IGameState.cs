using System;

namespace EscapeThe90s.Contracts
{
    /// <summary>
    /// Lets features track and request game-state transitions without referencing TL1.
    /// </summary>
    public interface IGameState
    {
        /// <summary>
        /// The active state, or None before the first state is entered.
        /// </summary>
        GameStateType CurrentStateType { get; }

        /// <summary>
        /// Raised once after every successful transition.
        /// </summary>
        event Action<GameStateChange> OnStateTypeChanged;

        void RequestStart();
        void RequestPause();
        void RequestResume();
        void RequestGameOver();
        void RequestRestart();
        void RequestReturnToMenu();
    }
}
