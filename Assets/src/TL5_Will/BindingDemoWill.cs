using UnityEngine;

public class BindingDemoWill : MonoBehaviour
{
    UpgradeEffect upgrd = new HealthEffect();

    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 400, 30), upgrd.Apply());
        if (GUI.Button(new Rect(20, 60, 160, 30), "Swap"))
            upgrd = (upgrd is HealthEffect) ? new UpgradeEffect() : new HealthEffect();
    }
}

