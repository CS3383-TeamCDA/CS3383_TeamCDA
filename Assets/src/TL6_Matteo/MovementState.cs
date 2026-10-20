
using UnityEngine;
using StateLogic = Assets.src.StateLogic;

public class MovementState : StateLogic.BaseState
{
    public override void EnterState(
        StateLogic.StateManager stateManager)
    {
    }

    public virtual string Execute()
    {
        Debug.Log("MovementState: Execute");
        return "MovementState: Execute";
    }

    public override void ExitState(
        StateLogic.StateManager stateManager)
    {
    }

    public override void UpdateState(
        StateLogic.StateManager stateManager)
    {
        Execute();
    }

    public virtual void FixedTick(PlayerStateManager manager)
    {
    }
}
