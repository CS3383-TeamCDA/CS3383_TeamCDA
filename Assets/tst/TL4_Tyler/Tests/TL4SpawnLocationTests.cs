using NUnit.Framework;
using UnityEngine;

public class TL4SpawnLocationTests
{
    private GameObject root;
    private EnemySpawner spawner;
    private Camera camera;
    private EnemySpawnPoint point;
    [SetUp] public void SetUp()
    {
        root = new GameObject("Spawn test");
        spawner = root.AddComponent<EnemySpawner>();
        var view = new GameObject("Camera"); view.transform.SetParent(root.transform);
        camera = view.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5; camera.aspect = 1;
        camera.transform.position = new Vector3(0, 0, -10);
        spawner.ConfigureCamera(camera);
        var location = new GameObject("Designed location"); location.transform.SetParent(root.transform);
        location.transform.position = new Vector3(12, 0, 0);
        point = location.AddComponent<EnemySpawnPoint>();
        spawner.ConfigureSpawnPoints(new[]{point.transform});
        spawner.SetSpawnRate(1);
    }
    [TearDown] public void TearDown() { Object.DestroyImmediate(root); }

    [Test] public void TypedPointUsesDesignedLocationAndLevelStats()
    {
        spawner.SetSpawnLevel(4);
        EnemyBase spawned = null;
        spawner.OnEnemySpawned += enemy => spawned = enemy;
        spawner.Tick(1);
        Assert.IsNotNull(spawned);
        Assert.AreEqual(point.transform.position, spawned.transform.position);
        Assert.AreEqual(EnemyType.Ground, spawned.Type);
        Assert.AreEqual(160, spawned.MaxHealth);
        Assert.AreEqual(11, spawned.CoinReward);
        Assert.AreEqual(16, spawned.MeleeDamage);
        Assert.AreEqual(14, spawned.RangedDamage);
        Assert.AreSame(root.transform, spawned.transform.parent);
    }

    [Test] public void VisibleFootprintAndMissingCameraPreventSpawns()
    {
        point.transform.position = new Vector3(5.5f, 0, 0);
        Assert.IsFalse(spawner.TrySpawnAt(point, out _));
        Object.DestroyImmediate(camera.gameObject);
        point.transform.position = new Vector3(20, 0, 0);
        var fallback = Camera.main;
        if (fallback != null) fallback.tag = "Untagged";
        try { Assert.IsFalse(spawner.TrySpawnAt(point, out _)); }
        finally { if (fallback != null) fallback.tag = "MainCamera"; }
    }

    [Test] public void OccupiedAndDisabledPointsAreSkipped()
    {
        Assert.IsTrue(spawner.TrySpawnAt(point, out var enemy));
        Assert.IsFalse(spawner.TrySpawnAt(point, out _));
        Object.DestroyImmediate(enemy.gameObject);
        point.enabled = false;
        Assert.IsFalse(spawner.TrySpawnAt(point, out _));
        point.enabled = true;
        Assert.IsTrue(spawner.TrySpawnAt(point, out _));
    }

    [Test] public void CameraMovementChangesEligibility()
    {
        camera.transform.position = new Vector3(12, 0, -10);
        Assert.IsFalse(spawner.TrySpawnAt(point, out _));
        camera.transform.position = new Vector3(0, 0, -10);
        Assert.IsTrue(spawner.TrySpawnAt(point, out _));
    }

    [Test] public void AboveScreenLocationsAreAllowedAndAliveCapIsRespected()
    {
        point.transform.position = new Vector3(0, 10, 0);
        spawner.ConfigureSpawnPoints(new[]{point.transform}, 1);
        Assert.IsTrue(spawner.TrySpawnAt(point, out _));
        point.transform.position = new Vector3(20, 0, 0);
        Assert.IsFalse(spawner.TrySpawnAt(point, out _));
    }
}
