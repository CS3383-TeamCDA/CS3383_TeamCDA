using UnityEngine;

public class MagnetEffect : PowerUpEffect
{
    // public override string Apply()
    // {
    //     Debug.Log("Magnet Apply");
    //     return "Magnet Apply";
    // }
    public override void Remove()
    {
        Debug.Log("Magnet Remove");
    }
    public override void CreateSnapshot()
    {
        Debug.Log("Magnet CreateSnapshot");
    }
}