using UnityEngine;

public class MovementState
{
    //PlayerController controller;

    public virtual string Execute()
    {
        Debug.Log("MovementState: Execute");
        return "MovementState: Execute";
    }
}
