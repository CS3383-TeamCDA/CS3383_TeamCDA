using UnityEngine;

namespace EscapeThe90s.GameStates
{
    public class PlayingState : GameState
    {
        public override void Enter()
        {
            Time.timeScale = 1f;
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