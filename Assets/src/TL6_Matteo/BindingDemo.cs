using UnityEngine;

public class BindingDemo : MonoBehaviour
{
    MovementState movement = new RunningState();
    
    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 400, 30), movement.Execute());
        if (GUI.Button(new Rect(20, 60, 160, 30), "Swap"))
            movement = (movement is RunningState) ? new MovementState() : new RunningState();
    }
}
