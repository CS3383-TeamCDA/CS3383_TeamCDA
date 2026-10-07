using System.Diagnostics;

namespace EscapeThe90s.GameStates
{
    public class PlayingState : GameState
    {
        public override string Enter()
        {
            //Debug.WriteLine("Playing State Enter Override");
            return "Playing State Enter Override";
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