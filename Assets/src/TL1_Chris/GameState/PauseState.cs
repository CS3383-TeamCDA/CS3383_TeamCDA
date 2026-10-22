using UnityEngine;

namespace EscapeThe90s.GameStates
{
    public class PauseState : GameState
    {
        public override void Enter()
        {
            Time.timeScale = 0f;
        }

        public override void Update()
        {
            // Code here
        }

        public override void Exit()
        {
            // Code here
        }
    }
}