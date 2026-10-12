using StateLogic = Assets.src.StateLogic;
using UnityEditor.Experimental;
using UnityEngine;
public class MovementState : StateLogic.BaseState
{   //PlayerController controller;
    public override void EnterState(StateLogic.StateManager stateManager)
    {
        throw new System.NotImplementedException();
    }

    public virtual string Execute()
    {
        Debug.Log("MovementState: Execute");
        return "MovementState: Execute";
    }

    public override void ExitState(StateLogic.StateManager stateManager)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateState(StateLogic.StateManager stateManager)
    {
        Execute();
    }
}
