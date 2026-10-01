using System.Diagnostics;

namespace EscapeThe90s.GameStates
{
    public class PauseState : GameState
    {
        public override void Enter()
        {
            Debug.WriteLine("Paused State Enter Override");
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