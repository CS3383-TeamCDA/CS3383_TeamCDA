using Assets.src.StateLogic;
using UnityEditor.Experimental;
using UnityEngine;
public class MovementState : Assets.src.StateLogic.BaseState
{   //PlayerController controller;
    public override void EnterState(StateManager stateManager)
    {
        throw new System.NotImplementedException();
    }

    public virtual string Execute()
    {
        Debug.Log("MovementState: Execute");
        return "MovementState: Execute";
    }

    public override void ExitState(StateManager stateManager)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateState(StateManager stateManager)
    {
        Execute();
    }
}
