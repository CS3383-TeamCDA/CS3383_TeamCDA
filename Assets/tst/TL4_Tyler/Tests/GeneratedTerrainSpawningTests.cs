#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GeneratedTerrainSpawningTests
{
    private Scene scene;

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (EscapeThe90s.GameStateManager.Instance != null)
            Object.Destroy(EscapeThe90s.GameStateManager.Instance.gameObject);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.CreateScene("AfterTerrainSpawning");
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator MvpSpawnsAnimatedEnemiesOffCameraAndRetiresUnloadedTerrain()
    {
        scene = EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Scenes/TL4_Tyler/EnemySpawningMvp.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        EscapeThe90s.GameStateManager.Instance.RequestStart();
        var population = Object.FindAnyObjectByType<GeneratedTerrainEnemyPopulation>();
        Assert.IsNotNull(population, "TL4 scene must contain its terrain adapter.");
        var spawner = population.GetComponent<EnemySpawner>();
        EnemyBase first = null;
        Vector3 firstSpawnPosition = Vector3.zero;
        bool allOffScreen = true;
        int turretsSpawned = 0;
        int flyersSpawned = 0;
        int timedSpawns = 0;
        spawner.OnEnemySpawned += enemy =>
        {
            timedSpawns++;
            if (enemy is TurretHazard) turretsSpawned++;
            if (enemy is FlyingEnemy) flyersSpawned++;
            if (first == null && enemy is GroundEnemy)
            {
                first = enemy;
                firstSpawnPosition = enemy.transform.position;
            }
            allOffScreen &= spawner.IsOffScreen(enemy.transform.position);
        };
        // Exercise normal LateUpdate spawning across multiple marker refreshes.
        yield return new WaitForSeconds(2.2f);
        Assert.Greater(population.SpawnPointCount, 0, "Generated ground needs clear spawn locations.");
        Assert.Greater(timedSpawns, 0, "Normal timed spawning must survive periodic marker refreshes.");
        // The first two spawns may now be flyers/turrets. Fill the mixed population before selecting a ground actor.
        for (int i = 0; i < 30; i++) spawner.Tick(1);
        Assert.IsNotNull(first, "The mixed population must include ground enemies.");
        Assert.IsNotNull(first.GetComponent<SpriteRenderer>().sprite);
        Assert.IsNotNull(first.GetComponent<Animator>().runtimeAnimatorController);
        Assert.AreEqual(EnemyType.Ground, first.Type);
        Assert.AreEqual(1, first.Level);
        var player = (Transform)typeof(GeneratedTerrainEnemyPopulation)
            .GetField("player", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(population);
        // Off-camera spawns can now legitimately be outside their runtime awareness range.
        // Approach inside every possible awareness radius before expecting pursuit.
        player.position = first.transform.position + Vector3.left * 4;
        float pursuitDeadline = Time.time + 6;
        while (first != null && Mathf.Abs(first.transform.position.x - firstSpawnPosition.x) <= .1f && Time.time < pursuitDeadline)
            yield return null;
        Assert.Greater(Mathf.Abs(first.transform.position.x - firstSpawnPosition.x), .1f,
            "Spawned ground enemies should pursue the scene's player.");
        Assert.IsTrue(allOffScreen);
        Assert.LessOrEqual(spawner.AliveCount, 8);
        Assert.Greater(turretsSpawned, 0, "The scene must spawn turrets alongside ground enemies.");
        Assert.Greater(flyersSpawned, 0, "The scene must include airborne enemy spawns.");

        first.transform.position += Vector3.up * 3;
        population.RefreshSpawnPoints();
        yield return null;
        Assert.IsTrue(first != null, "Airborne enemies must survive population refreshes.");

        player.position += Vector3.right * 200;
        yield return null; // Let TL2 stream the new chunk interval.
        population.RefreshSpawnPoints();
        yield return null; // Finish deferred enemy destruction.
        Assert.IsTrue(first == null, "Enemies must retire when their supporting terrain unloads.");
    }
}
#endif
