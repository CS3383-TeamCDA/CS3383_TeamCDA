using UnityEngine;

public class UpgradeEffect
{
    public virtual string Apply()
    {
        Debug.Log("Base Apply");
        return "Base Apply";
    }
}
