using UnityEngine;

public class HealthEffect : UpgradeEffect
{
    public override string Apply()
    {
        Debug.Log("Health Apply");
        return "Health Apply";
    }
}
