using StateLogic = Assets.src.StateLogic;

public class PlayerStateManager : StateLogic.StateManager
{
    public TempPlayerController Player { get; private set; }

    private MovementState _activeState;

    public PlayerStateManager(TempPlayerController player)
    {
        Player = player;
    }

    public override string Start()
    {
        ChangeState(new RunningState());
        return "Player state initialized";
    }

    public override string Update()
    {
        _activeState?.UpdateState(this);
        return _activeState?.GetType().Name ?? "No state";
    }

    public void FixedUpdate()
    {
        _activeState?.FixedTick(this);
    }

    public void ChangeState(MovementState newState)
    {
        SwitchState(newState);
        _activeState = newState;
    }
}
