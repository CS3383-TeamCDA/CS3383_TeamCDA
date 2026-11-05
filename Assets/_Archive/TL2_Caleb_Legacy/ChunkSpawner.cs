using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private Vector3 chunkSize = new Vector3(16f, 0f, 10f);
    [SerializeField] private Transform parentTransform;
    

    public void SpawnChunkAtCoordinates(int xCoord, int yCoord)
    {
        Vector3 spawnPosition = new Vector3(xCoord * chunkSize.x, 0f, yCoord * chunkSize.z);
        GameObject newChunk = Instantiate(chunkPrefab, spawnPosition, Quaternion.identity, parentTransform);
        newChunk.name = $"Chunk_{xCoord}_{yCoord}";
    }
}