using UnityEngine;

public abstract class SpawnPattern : ScriptableObject
{
    public abstract int GetCount(int wave);
}