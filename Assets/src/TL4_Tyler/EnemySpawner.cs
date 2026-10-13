using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Ground,
    Flying,
    Turret,
    Aim
}

[DefaultExecutionOrder(10000)]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField, Min(0)] private float spawnRate;
    [SerializeField, Min(1)] private int maxAlive = 8;
    [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();
    [SerializeField] private EnemyBase[] prefabs = new EnemyBase[4];
    private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();
    [SerializeField] private Camera gameplayCamera;
    [SerializeField, Min(0)] private float screenMargin = 1f;
    private float elapsed;
    private int nextPoint;
    private int nextType;

    public int AliveCount { get { activeEnemies.RemoveAll(e => e == null); return activeEnemies.Count; } }
    public event Action<EnemyBase> OnEnemySpawned;

    // Rate is enemies per second. Zero pauses spawning without accumulating debt.
    public void ConfigureSpawnPoints(Transform[] points, int limit = 8)
    {
        spawnPoints = points ?? Array.Empty<Transform>();
        maxAlive = Mathf.Max(1, limit);
        nextPoint = 0;
    }

    public void ConfigureCamera(Camera camera) { gameplayCamera = camera; }

    // Horizontal exclusion also covers flying enemies raised after creation.
    public bool IsOffScreen(Vector3 position)
    {
        Camera camera = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (camera == null) return false;
        Vector3 left = camera.WorldToViewportPoint(position - camera.transform.right * screenMargin);
        Vector3 right = camera.WorldToViewportPoint(position + camera.transform.right * screenMargin);
        return left.z > 0 && right.z > 0 && (right.x < 0 || left.x > 1);
    }

    // Run after the camera follows the player, using its final view for this frame.
    private void LateUpdate() { Tick(Time.deltaTime); }

    public void Tick(float deltaTime)
    {
        if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0) return;
        if (spawnRate <= 0 || spawnPoints.Length == 0 || AliveCount >= maxAlive)
        {
            elapsed = 0;
            return;
        }
        elapsed += deltaTime;
        float interval = 1f / spawnRate;
        // At most one spawn per frame: no burst after a hitch or while capped.
        if (elapsed < interval) return;
        elapsed = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[nextPoint++ % spawnPoints.Length];
            if (point == null || !point.gameObject.activeInHierarchy || !IsOffScreen(point.position)) continue;
            CreateEnemy((EnemyType)(nextType++ % 4), point.position);
            break;
        }
    }

    public event Action<EnemyType, Vector3> OnEnemyDefeated;

    public void SetSpawnRate(float rate)
    {
        if (float.IsNaN(rate) || float.IsInfinity(rate)) throw new ArgumentOutOfRangeException(nameof(rate));
        spawnRate = Mathf.Max(0.0f, rate);
        elapsed = 0;
    }

    public EnemyBase CreateEnemy(EnemyType type)
    {
        return CreateEnemy(type, transform.position);
    }

    public EnemyBase CreateEnemy(EnemyType type, Vector3 position)
    {
        if (type != EnemyType.Ground && type != EnemyType.Flying && type != EnemyType.Turret && type != EnemyType.Aim)
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported enemy type.");
        }

        EnemyBase prefab = prefabs != null && (int)type < prefabs.Length ? prefabs[(int)type] : null;
        EnemyBase enemy;
        if (prefab != null)
        {
            enemy = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            GameObject enemyObject = new GameObject(type + " Enemy");
            enemyObject.transform.position = position;
            switch (type)
            {
                case EnemyType.Ground:
                    enemy = enemyObject.AddComponent<GroundEnemy>();
                    break;
                case EnemyType.Flying:
                    enemy = enemyObject.AddComponent<FlyingEnemy>();
                    break;
                case EnemyType.Turret:
                    enemy = enemyObject.AddComponent<TurretHazard>();
                    break;
                default:
                    enemy = enemyObject.AddComponent<AimEnemy>();
                    break;
            }

        }
        enemy.Type = type;
        enemy.Defeated += HandleEnemyDefeated;
        activeEnemies.Add(enemy);
        OnEnemySpawned?.Invoke(enemy);
        return enemy;
    }

    private void OnDestroy()
    {
        foreach (EnemyBase enemy in activeEnemies)
            if (enemy != null) enemy.Defeated -= HandleEnemyDefeated;
    }

    private void HandleEnemyDefeated(EnemyBase enemy)
    {
        enemy.Defeated -= HandleEnemyDefeated;
        activeEnemies.Remove(enemy);
        if (this != null)
        {
            OnEnemyDefeated?.Invoke(enemy.Type, enemy.transform.position);
        }
    }
}
