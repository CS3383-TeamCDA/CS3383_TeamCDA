using System;
using UnityEngine;

// Attach to the generated-room parent, or assign that parent as markerRoot.
// Call RefreshSpawnPoints after generating/replacing rooms (also runs on Start).
[RequireComponent(typeof(EnemySpawner))]
public class EnemyLevelPopulation : MonoBehaviour
{
    [SerializeField] private Transform markerRoot;
    [SerializeField, Min(1)] private int difficultyLevel = 1;
    [SerializeField, Min(0)] private float spawnRate = 1;
    [SerializeField, Min(1)] private int maxAlive = 8;

    public int DifficultyLevel => Mathf.Max(1, difficultyLevel);

    public void SetDifficultyLevel(int level)
    {
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        difficultyLevel = level;
        GetComponent<EnemySpawner>().SetSpawnLevel(level);
    }

    private void Start() { RefreshSpawnPoints(); }

    public void RefreshSpawnPoints()
    {
        Transform root = markerRoot != null ? markerRoot : transform;
        var markers = root.GetComponentsInChildren<EnemySpawnPoint>(true);
        var points = new Transform[markers.Length];
        for (int i = 0; i < markers.Length; i++) points[i] = markers[i].transform;
        var spawner = GetComponent<EnemySpawner>();
        spawner.ConfigureSpawnPoints(points, maxAlive);
        spawner.SetSpawnLevel(DifficultyLevel);
        spawner.SetSpawnRate(spawnRate);
    }
}
