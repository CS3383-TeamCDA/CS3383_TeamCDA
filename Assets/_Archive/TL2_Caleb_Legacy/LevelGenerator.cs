using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private ChunkSpawner StartChunkGen;
    [SerializeField] private ChunkSpawner MiddleChunkGen;
    [SerializeField] private ChunkSpawner EndChunkGen;

    private void Awake()
    {
        if (StartChunkGen == null || MiddleChunkGen == null || EndChunkGen == null)
        {
            Debug.LogError("A chunk gen is missing!");
        }
    }

    public void GenerateLevel()
    {
        StartChunkGen.SpawnChunkAtCoordinates(0,0);
        MiddleChunkGen.SpawnChunkAtCoordinates(1,0);
        EndChunkGen.SpawnChunkAtCoordinates(2,0);
    }
}
