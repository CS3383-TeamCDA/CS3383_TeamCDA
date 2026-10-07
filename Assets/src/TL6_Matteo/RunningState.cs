using UnityEngine;

public class RunningState : MovementState
{
    public override string Execute()
    {
        Debug.Log("RunningState: Execute");
        return "RunningState: Execute";
    }
}
