using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Transform _parent;

    public event System.Action<GameObject> ObjectSpawned;

    public GameObject Spawn()
    {
        var instance = Instantiate(_prefab, GetSpawnPosition(), Quaternion.identity, _parent);
        ObjectSpawned?.Invoke(instance);
        return instance;
    }

    private Vector3 GetSpawnPosition()
    {   
        return new Vector3(0, 0, 0);
    }
}
