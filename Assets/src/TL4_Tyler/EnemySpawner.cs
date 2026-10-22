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
    [SerializeField, Min(1)] private int spawnLevel = 1;
    public int SpawnLevel => Mathf.Max(1, spawnLevel);

    public void SetSpawnLevel(int level)
    {
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        spawnLevel = level;
    }
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

    public bool IsOffScreen(Vector3 position)
    {
        return IsOffScreen(new Bounds(position, Vector3.zero));
    }

    // Test the whole designer-specified footprint plus margin, not just its origin.
    public bool IsOffScreen(Bounds bounds)
    {
        Camera camera = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (camera == null) return false;
        bounds.Expand(Mathf.Max(0, screenMargin) * 2);
        Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 viewport = camera.WorldToViewportPoint(corner);
            if (viewport.z < camera.nearClipPlane || viewport.z > camera.farClipPlane) return false;
            minimum = Vector2.Min(minimum, viewport);
            maximum = Vector2.Max(maximum, viewport);
        }
        return maximum.x < 0 || minimum.x > 1 || maximum.y < 0 || minimum.y > 1;
    }

    public bool TrySpawnAt(EnemySpawnPoint point, out EnemyBase enemy)
    {
        enemy = null;
        if (point == null || !point.IsAvailable || AliveCount >= maxAlive || !IsOffScreen(point.ClearanceBounds)) return false;
        foreach (EnemyBase active in activeEnemies)
        {
            if (active == null) continue;
            var renderer = active.GetComponentInChildren<Renderer>();
            if (renderer != null ? point.ClearanceBounds.Intersects(renderer.bounds) :
                ((Vector2)(active.transform.position - point.transform.position)).sqrMagnitude <
                point.ClearanceBounds.size.sqrMagnitude) return false;
        }
        enemy = CreateEnemyInternal(point.Type, point.transform.position, point.Prefab);
        // Level-owned enemies disappear with their generated room on restart.
        enemy.transform.SetParent(point.transform.parent, true);
        return true;
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
            Transform point = spawnPoints[nextPoint];
            nextPoint = (nextPoint + 1) % spawnPoints.Length;
            if (point == null || !point.gameObject.activeInHierarchy) continue;
            var marker = point.GetComponent<EnemySpawnPoint>();
            if (marker != null)
            {
                if (TrySpawnAt(marker, out _)) break;
                continue;
            }
            // Preserve untyped legacy/demo points; generated levels use typed markers.
            if (!IsOffScreen(point.position)) continue;
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
        return CreateEnemyInternal(type, position, null);
    }

    private EnemyBase CreateEnemyInternal(EnemyType type, Vector3 position, EnemyBase overridePrefab)
    {
        if (type != EnemyType.Ground && type != EnemyType.Flying && type != EnemyType.Turret && type != EnemyType.Aim)
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported enemy type.");
        }

        EnemyBase prefab = overridePrefab != null ? overridePrefab :
            (prefabs != null && (int)type < prefabs.Length ? prefabs[(int)type] : null);
        EnemyBase enemy;
        if (prefab != null)
        {
            if (prefab.Type != type) throw new InvalidOperationException("Enemy prefab type does not match its spawn slot.");
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
        enemy.SetLevel(SpawnLevel);
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
