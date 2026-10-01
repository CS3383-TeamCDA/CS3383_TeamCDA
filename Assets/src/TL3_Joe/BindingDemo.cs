using UnityEngine;

public class BindingDemo : MonoBehaviour
{
    PowerUpEffect power = new SpeedBoostEffect();

    /// <summary>
    /// OnGUI is called for rendering and handling GUI events.
    /// This function can be called multiple times per frame (one call per event).
    /// </summary>
    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 400, 30), power.Apply());
        if(GUI.Button(new Rect(20, 60, 160, 30), "Swap"))
            power = (power is SpeedBoostEffect) ? new PowerUpEffect() : new SpeedBoostEffect();
    }
}
