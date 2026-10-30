using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// TL4 adapter for the MVP's streamed collision tiles; no changes to TL2 generation.
[DefaultExecutionOrder(9000)]
[RequireComponent(typeof(EnemySpawner))]
public class GeneratedTerrainEnemyPopulation : MonoBehaviour
{
    [SerializeField] private Tilemap collisionTilemap;
    [SerializeField] private Transform player;
    [SerializeField] private EnemySpawnPoint groundSpawnPointPrefab;
    [SerializeField] private EnemySpawnPoint turretSpawnPointPrefab;
    [SerializeField] private EnemySpawnPoint flyingSpawnPointPrefab;
    [SerializeField, Min(2)] private int flyingEvery = 4;
    [SerializeField, Min(1)] private float flyingSpawnHeight = 3;
    [SerializeField, Min(2)] private int turretEvery = 3;
    [SerializeField, Min(1)] private int difficultyLevel = 1;
    [SerializeField, Min(0)] private float spawnRate = 1;
    [SerializeField, Min(1)] private int maxAlive = 8;
    [SerializeField, Min(2)] private int spacing = 6;
    [SerializeField, Min(8)] private int scanRadius = 64;
    [SerializeField, Min(.1f)] private float refreshInterval = .5f;

    private readonly Dictionary<Vector3Int, EnemySpawnPoint> markers = new Dictionary<Vector3Int, EnemySpawnPoint>();
    private readonly HashSet<Vector3Int> candidates = new HashSet<Vector3Int>();
    private readonly List<Vector3Int> staleMarkers = new List<Vector3Int>();
    private readonly List<EnemyBase> enemies = new List<EnemyBase>();
    private EnemySpawner spawner;
    private float timer;
    private float configuredRate = -1;
    public int SpawnPointCount => markers.Count;

    public void Configure(Tilemap terrain, Transform target, EnemySpawnPoint markerPrefab)
    {
        collisionTilemap = terrain;
        player = target;
        groundSpawnPointPrefab = markerPrefab;
    }

    private void Awake() { spawner = GetComponent<EnemySpawner>(); }
    private void OnEnable() { spawner.OnEnemySpawned += TrackEnemy; }
    private void OnDisable() { spawner.OnEnemySpawned -= TrackEnemy; }
    private void TrackEnemy(EnemyBase enemy)
    {
        enemies.Add(enemy);
        if (enemy is GroundEnemy ground) ground.SetTarget(player);
        if (enemy is TurretHazard turret) turret.SetTarget(player);
        if (enemy is FlyingEnemy flying) flying.SetTarget(player);
    }

    private void LateUpdate()
    {
        timer -= Time.deltaTime;
        if (timer > 0) return;
        timer = Mathf.Max(.1f, refreshInterval);
        RefreshSpawnPoints();
    }

    public void RefreshSpawnPoints()
    {
        if (collisionTilemap == null || player == null || groundSpawnPointPrefab == null)
        {
            spawner.ConfigureSpawnPoints(null);
            return;
        }

        // cellBounds can retain old extents after tiles unload, so bound the scan to the player.
        int centerX = collisionTilemap.WorldToCell(player.position).x;
        int radius = Mathf.Max(8, scanRadius);
        var bounds = collisionTilemap.cellBounds;
        int first = Mathf.Max(bounds.xMin, centerX - radius);
        int last = Mathf.Min(bounds.xMax - 1, centerX + radius);
        int step = Mathf.Max(2, spacing);
        candidates.Clear();
        for (int x = first; x <= last; x++)
        {
            if (x % step != 0) continue;
            var markerPrefab = turretSpawnPointPrefab != null && (x / step) % Mathf.Max(2, turretEvery) == 0
                ? turretSpawnPointPrefab : groundSpawnPointPrefab;
            if (flyingSpawnPointPrefab != null && (x / step) % Mathf.Max(2, flyingEvery) == 0)
                markerPrefab = flyingSpawnPointPrefab;
            // Require headroom inside the generated row; don't spawn on the boundary wall.
            for (int y = bounds.yMax - 3; y >= bounds.yMin; y--)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!collisionTilemap.HasTile(cell)) continue;
                if (collisionTilemap.HasTile(cell + Vector3Int.up)) continue;
                Vector3 position = SurfacePosition(cell);
                if (markerPrefab.Type == EnemyType.Flying) position += Vector3.up * flyingSpawnHeight;
                Bounds clearance = markerPrefab.ClearanceBounds;
                clearance.center += position - markerPrefab.transform.position;
                if (!IsClear(clearance)) continue;
                candidates.Add(cell);
                if (!markers.ContainsKey(cell))
                {
                    var marker = Instantiate(markerPrefab, position, Quaternion.identity, transform);
                    marker.name = marker.Type + " spawn " + cell;
                    markers.Add(cell, marker);
                }
                break;
            }
        }

        staleMarkers.Clear();
        foreach (var pair in markers)
            if (!candidates.Contains(pair.Key)) staleMarkers.Add(pair.Key);
        foreach (var cell in staleMarkers)
        {
            // Disable immediately so this frame's spawner cannot use a pending destruction.
            markers[cell].gameObject.SetActive(false);
            Destroy(markers[cell].gameObject);
            markers.Remove(cell);
        }

        // Keep jumping/falling enemies alive while their terrain column remains loaded.
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (enemy != null)
            {
                var support = collisionTilemap.WorldToCell(enemy.transform.position - Vector3.up * .1f);
                if (!enemy.IsDefeated && HasTerrainColumn(support.x) &&
                    enemy.transform.position.y > collisionTilemap.CellToWorld(
                        new Vector3Int(support.x, bounds.yMin - 8, 0)).y &&
                    Mathf.Abs(support.x - centerX) <= radius) continue;
                Destroy(enemy.gameObject);
            }
            enemies.RemoveAt(i);
        }

        var points = new Transform[markers.Count];
        int index = 0;
        foreach (var marker in markers.Values) points[index++] = marker.transform;
        spawner.ConfigureSpawnPoints(points, maxAlive);
        spawner.SetSpawnLevel(Mathf.Max(1, difficultyLevel));
        if (configuredRate != spawnRate)
        {
            spawner.SetSpawnRate(spawnRate);
            configuredRate = spawnRate;
        }
    }

    private Vector3 SurfacePosition(Vector3Int cell)
    {
        Vector3 bottom = collisionTilemap.CellToWorld(cell);
        Vector3 top = collisionTilemap.CellToWorld(cell + Vector3Int.up);
        Vector3 right = collisionTilemap.CellToWorld(cell + Vector3Int.right);
        return top + (right - bottom) * .5f + Vector3.up * .05f;
    }

    private bool HasTerrainColumn(int x)
    {
        var bounds = collisionTilemap.cellBounds;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
            if (collisionTilemap.HasTile(new Vector3Int(x, y, 0))) return true;
        return false;
    }

    private bool IsClear(Bounds bounds)
    {
        Vector3Int min = collisionTilemap.WorldToCell(bounds.min + Vector3.one * .01f);
        Vector3Int max = collisionTilemap.WorldToCell(bounds.max - Vector3.one * .01f);
        for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
                if (collisionTilemap.HasTile(new Vector3Int(x, y, 0))) return false;
        return true;
    }
}
