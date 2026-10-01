using UnityEngine;

public class JumpingState : MovementState
{
    public override string Execute()
    {
        Debug.Log("JumpingState: Execute");
        return "JumpingState: Execute";
    }
}
