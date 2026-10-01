using UnityEngine;

public class PowerUpEffect
{
    public float Duration { get; protected set; }
    public bool IsPermanent { get; protected set; }
    public int MaxStacks { get; protected set; }

    public virtual string Apply()
    {
        Debug.Log("Base Apply");
        return "Base Apply";
    }
    //  public string Apply()
    // {
    //     Debug.Log("Base Apply");
    //     return "Base Apply";
    // }
    public virtual void Remove()
    {
        Debug.Log("Base Remove");
    }
    public virtual void CreateSnapshot()
    {
        Debug.Log("Base CreateSnapshot");
    }

}
