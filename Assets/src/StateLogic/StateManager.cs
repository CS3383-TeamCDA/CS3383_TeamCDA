namespace Assets.src.StateLogic
{
    public abstract class StateManager
    {
        private BaseState currentState;

        public abstract string Start();
        public abstract string Update();
        public void SwitchState(BaseState newState)
        {
            currentState?.ExitState(this);
            currentState = newState;
            currentState.EnterState(this);
        }
    }
}