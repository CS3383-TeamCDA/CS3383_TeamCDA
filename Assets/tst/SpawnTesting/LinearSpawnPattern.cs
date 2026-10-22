using UnityEngine;

[CreateAssetMenu(menuName = "Spawning/Linear Pattern")]
public class LinearSpawnPattern : SpawnPattern
{
    [SerializeField] private int _start = 1;
    [SerializeField] private int _increment = 1;

    public override int GetCount(int wave) => _start + _increment * wave;
}