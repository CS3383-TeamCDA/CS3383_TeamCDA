using UnityEngine;

public class SpeedEffect : UpgradeEffect
{
    public override string Apply()
    {
        Debug.Log("Speed Apply");
        return "Speed Apply";
    }
}
