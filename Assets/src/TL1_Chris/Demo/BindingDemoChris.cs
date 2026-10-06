using UnityEngine;

public class BindingDemoChris : MonoBehaviour
{
    Enemy current = new Dragon(); // declared: Enemy · actual: Dragon​

    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 400, 30), current.Attack());
        if (GUI.Button(new Rect(20, 60, 160, 30), "Swap"))
            current = (current is Dragon) ? new Enemy() : new Dragon();
    }
}
