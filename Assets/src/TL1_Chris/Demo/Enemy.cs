using UnityEngine;

public class Enemy // YOUR superclass name​
{
    public virtual string Attack() // virtual = overridable​
    {
        return "generic attack"; // RETURN a value​
    }
}
