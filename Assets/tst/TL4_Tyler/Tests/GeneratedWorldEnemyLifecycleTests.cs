#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using EscapeThe90s.Contracts;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>Exercises enemy composition and cleanup against TL2's active streamed-world demo.</summary>
public class GeneratedWorldEnemyLifecycleTests
{
    private Scene scene;
    private object worldGenSettings;
    private FieldInfo minimumStreetChunksField;
    private FieldInfo maximumStreetChunksField;
    private int originalMinimumStreetChunks;
    private int originalMaximumStreetChunks;

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (worldGenSettings != null && minimumStreetChunksField != null && maximumStreetChunksField != null)
        {
            minimumStreetChunksField.SetValue(worldGenSettings, originalMinimumStreetChunks);
            maximumStreetChunksField.SetValue(worldGenSettings, originalMaximumStreetChunks);
            worldGenSettings = null;
        }
        if (EscapeThe90s.GameStateManager.Instance != null)
            Object.Destroy(EscapeThe90s.GameStateManager.Instance.gameObject);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.CreateScene("AfterGeneratedWorldEnemyLifecycle");
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator GeneratedWorldSpawnsMixedEnemiesAndRetiresThemWithUnloadedChunks()
    {
        scene = EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Scenes/GeneratedWorldGameDemo.unity",
            new LoadSceneParameters(LoadSceneMode.Single));

        float setupDeadline = Time.realtimeSinceStartup + 10f;
        while (EscapeThe90s.GameStateManager.Instance == null && Time.realtimeSinceStartup < setupDeadline)
            yield return null;

        Assert.IsNotNull(EscapeThe90s.GameStateManager.Instance, "The generated-world scene must install its game-state services.");
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        Assert.IsNotNull(spawner, "The generated-world scene must provide the team's enemy spawner.");

        MonoBehaviour demo = FindBehaviour("EscapeThe90s.WorldGen.Demo.WorldNetworkDemo");
        Assert.IsNotNull(demo, "The scene must contain WorldNetworkDemo.");
        FieldInfo settingsField = demo.GetType().GetField("settings", BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(settingsField, "WorldNetworkDemo must expose its world-generation settings.");
        worldGenSettings = settingsField.GetValue(demo);
        Assert.IsNotNull(worldGenSettings, "WorldNetworkDemo must reference WorldGenSettings.");
        minimumStreetChunksField = worldGenSettings.GetType().GetField("minNetworkStreetChunks");
        maximumStreetChunksField = worldGenSettings.GetType().GetField("maxNetworkStreetChunks");
        Assert.IsNotNull(minimumStreetChunksField);
        Assert.IsNotNull(maximumStreetChunksField);
        originalMinimumStreetChunks = (int)minimumStreetChunksField.GetValue(worldGenSettings);
        originalMaximumStreetChunks = (int)maximumStreetChunksField.GetValue(worldGenSettings);
        // Default streets are only 4-6 chunks long, too short to prove eviction beyond the
        // load/unload hysteresis. Use a longer test street without changing production settings.
        minimumStreetChunksField.SetValue(worldGenSettings, 14);
        maximumStreetChunksField.SetValue(worldGenSettings, 14);

        FieldInfo assignedStreetView = demo.GetType().GetField("streetView", BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(assignedStreetView, "WorldNetworkDemo must expose its assigned StreetView.");
        MonoBehaviour streetView = assignedStreetView.GetValue(demo) as MonoBehaviour;
        Assert.IsNotNull(streetView, "WorldNetworkDemo must have a StreetView.");

        PropertyInfo loadedChunksProperty = streetView.GetType().GetProperty("LoadedChunkCount");
        Assert.IsNotNull(loadedChunksProperty, "StreetView must expose its loaded chunk count.");
        PropertyInfo widthProperty = streetView.GetType().GetProperty("WidthCells");
        Assert.IsNotNull(widthProperty, "StreetView must expose the generated street width.");
        MethodInfo isChunkLoaded = streetView.GetType().GetMethod("IsChunkLoaded", BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(isChunkLoaded, "StreetView must expose chunk load state.");
        FieldInfo chunksField = streetView.GetType().GetField("_chunks", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(chunksField, "StreetView must track its loaded chunks.");

        var spawned = new List<EnemyBase>();
        HashSet<EnemyType> seenTypes = new HashSet<EnemyType>();
        System.Action<EnemyBase> onSpawned = enemy =>
        {
            if (enemy == null) return;
            spawned.Add(enemy);
            seenTypes.Add(enemy.Type);
        };
        spawner.OnEnemySpawned += onSpawned;

        EscapeThe90s.GameStateManager.Instance.RequestStart();
        float spawnDeadline = Time.realtimeSinceStartup + 12f;
        while ((spawned.Count == 0 || seenTypes.Count < 3 || (int)loadedChunksProperty.GetValue(streetView, null) == 0) &&
               Time.realtimeSinceStartup < spawnDeadline)
            yield return null;

        spawner.OnEnemySpawned -= onSpawned;
        Assert.Greater((int)loadedChunksProperty.GetValue(streetView, null), 0, "Gameplay should load generated street chunks.");
        Assert.Greater(spawned.Count, 0, "Generated chunks should create enemies through the team spawner.");
        Assert.IsTrue(seenTypes.Contains(EnemyType.Ground), "Generated content should include ground enemies.");
        Assert.IsTrue(seenTypes.Contains(EnemyType.Flying), "Generated content should include flying enemies.");
        Assert.IsTrue(seenTypes.Contains(EnemyType.Turret), "Generated content should include turrets.");

        EnemyBase groundEnemy = spawned.Find(enemy => enemy != null && enemy.Type == EnemyType.Ground);
        Assert.IsNotNull(groundEnemy, "At least one ground enemy should be created.");
        SpriteRenderer renderer = groundEnemy.GetComponent<SpriteRenderer>();
        Assert.IsNotNull(renderer);
        Assert.IsNotNull(renderer.sprite, "Generated enemies should use the team's animated enemy prefabs.");
        Animator animator = groundEnemy.GetComponent<Animator>();
        Assert.IsNotNull(animator);
        Assert.IsNotNull(animator.runtimeAnimatorController);

        var initialChunkIndices = new List<int>();
        var initiallyLoadedChunks = chunksField.GetValue(streetView) as System.Collections.IDictionary;
        Assert.IsNotNull(initiallyLoadedChunks);
        foreach (object chunkIndex in initiallyLoadedChunks.Keys)
            initialChunkIndices.Add((int)chunkIndex);
        Assert.Greater(initialChunkIndices.Count, 0, "The test should capture the chunks that own the spawned enemies.");

        Transform player = (Transform)demo.GetType().GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(demo);
        Assert.IsNotNull(player, "The generated-world demo should have activated its player.");
        FieldInfo followField = streetView.GetType().GetField("follow", BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(followField, "StreetView must expose its follow target.");
        Assert.AreSame(player, followField.GetValue(streetView), "The game should stream terrain around the player.");
        FieldInfo rootField = streetView.GetType().GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(rootField, "StreetView must have a generated street root.");
        GameObject streetRootObject = rootField.GetValue(streetView) as GameObject;
        Assert.IsNotNull(streetRootObject, "StreetView should have built its generated street root.");
        Transform streetRoot = streetRootObject.transform;
        float streetWidth = (int)widthProperty.GetValue(streetView, null);
        Assert.Greater(streetWidth, 192f, "The test should generate a street longer than the production 4-6 chunk default.");

        // Isolate the streamer's position input from player physics/input in this lifecycle test.
        // Put the test target at the generated street's local midpoint, away from its initial end.
        GameObject streamTarget = new GameObject("Enemy Lifecycle Stream Target");
        streamTarget.transform.position = player.position;
        followField.SetValue(streetView, streamTarget.transform);
        streamTarget.transform.position = streetRoot.TransformPoint(new Vector3(streetWidth * .5f, player.position.y, 0f));

        float unloadDeadline = Time.realtimeSinceStartup + 8f;
        while (initialChunkIndices.Exists(index => (bool)isChunkLoaded.Invoke(streetView, new object[] { index })) &&
               Time.realtimeSinceStartup < unloadDeadline)
            yield return null;

        Assert.IsFalse(initialChunkIndices.Exists(index => (bool)isChunkLoaded.Invoke(streetView, new object[] { index })),
            "Moving the stream target to the street midpoint should unload the original chunks.");
        // Chunk objects are destroyed at the end of Unity's frame after the recycle callback.
        yield return null;
        Assert.IsFalse(spawned.Exists(enemy => enemy != null),
            "Enemies owned by the old chunks should be destroyed when those chunks unload.");
    }

    private static MonoBehaviour FindBehaviour(string fullTypeName)
    {
        foreach (MonoBehaviour component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            if (component != null && component.GetType().FullName == fullTypeName) return component;
        return null;
    }
}
#endif
