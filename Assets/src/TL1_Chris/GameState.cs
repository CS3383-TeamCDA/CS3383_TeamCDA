using EscapeThe90s.State;

namespace EscapeThe90s.GameStates
{
    public abstract class GameState : IState
    {
        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void Exit() { }
    }
}
