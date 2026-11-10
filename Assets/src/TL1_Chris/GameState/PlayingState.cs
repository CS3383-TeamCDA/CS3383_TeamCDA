using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.GameStates
{
    public class PlayingState : GameState
    {
        public override GameStateType Type => GameStateType.Playing;

        public override void Enter()
        {
            // Set explicitly rather than restored: after a restart this state is entered straight from a frozen scene reload,
            // so there is no earlier "playing" value to go back to.
            // The cursor is left alone because gameplay aims with the mouse (e.g. DeleteExeAttack).
            Time.timeScale = 1f;
        }
    }
}
