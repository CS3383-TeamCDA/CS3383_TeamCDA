using UnityEngine;

public class DashingState : MovementState
{
    public override string Execute()
    {
        Debug.Log("DashingState: Execute");
        return "DashingState: Execute";
    }
}
