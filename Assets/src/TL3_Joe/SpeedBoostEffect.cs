using UnityEngine;

public class SpeedBoostEffect : PowerUpEffect
{
    public override string Apply()
    {
        Debug.Log("Speed Apply");
        return "Speed Apply";
    }
    // public string Apply()
    // {
    //     Debug.Log("Speed Apply");
    //     return "Speed Apply";
    // }
    public override void Remove()
    {
        Debug.Log("Speed Remove");
    }
    public override void CreateSnapshot()
    {
        Debug.Log("Speed CreateSnapshot");
    }
}
