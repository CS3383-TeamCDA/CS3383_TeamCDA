using System;
using System.Collections.Generic;
using EscapeThe90s.WorldGen;
using EscapeThe90s.WorldGen.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeThe90s.WorldGen.Demo.Editor
{
public static class IntegratedWorldDemoSetup
{
    const string SourceScenePath = "Assets/Scenes/TL5_Will/GameDemo.unity";
    const string OutputScenePath = "Assets/Scenes/GeneratedWorldGameDemo.unity";
    const string SettingsPath = "Assets/src/TL2_Caleb/WorldGenSettings.asset";
    const string ArtPath = "Assets/src/TL2_Caleb/Art/WorldGen/WorldArt.asset";
    const string PlayerPrefabPath = "Assets/prefabs/TL6_Matteo/PlayerTL6.prefab";

    [MenuItem("Escape the 90s/Create Integrated Generated World Demo")]
    public static void CreateIntegratedDemo()
    {
        if (AssetDatabase.LoadAssetAtPath<WorldGenSettings>(SettingsPath) == null ||
            AssetDatabase.LoadAssetAtPath<WorldArt>(ArtPath) == null)
            throw new InvalidOperationException("WorldGenSettings or WorldArt asset is missing from the active TL2_Assets folder.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OutputScenePath) != null)
            throw new InvalidOperationException(OutputScenePath + " already exists. Remove or rename it before regenerating this scene.");

        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, OutputScenePath))
            throw new InvalidOperationException("Could not create the integrated generated-world scene.");
        scene = SceneManager.GetActiveScene();

        DisableLegacySceneSystems(scene);
        WorldNetworkDemo demo = CreateWorldDemo(scene);
        ConfigureBuildSettings(OutputScenePath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = demo.gameObject;
        Debug.Log("Integrated generated-world demo created at " + OutputScenePath + ". It preserves the existing player, power-up, and enemy systems.");
    }

    static void DisableLegacySceneSystems(Scene scene)
    {
        foreach (MonoBehaviour component in FindSceneBehaviours(scene))
        {
            if (component == null) continue;
            string typeName = component.GetType().Name;
            if (typeName == "LevelGenerator" || typeName == "ChunkGenerator" ||
                typeName == "GeneratedTerrainEnemyPopulation")
                component.enabled = false;
        }

        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
            if (gameObject.scene == scene && gameObject.name == "Grid") gameObject.SetActive(false);
    }

    static WorldNetworkDemo CreateWorldDemo(Scene scene)
    {
        WorldGenSettings settings = AssetDatabase.LoadAssetAtPath<WorldGenSettings>(SettingsPath);
        WorldArt art = AssetDatabase.LoadAssetAtPath<WorldArt>(ArtPath);
        GameObject demoObject = new GameObject("Generated World Demo");
        SceneManager.MoveGameObjectToScene(demoObject, scene);
        WorldNetworkDemo demo = demoObject.AddComponent<WorldNetworkDemo>();
        demo.settings = settings;
        demo.art = art;
        demo.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        demo.enemyPrefabsByKind = FindEnemyPrefabs();
        demo.demoPlayerSpeed = 16f;
        demo.mapRadiusRegions = 2;

        foreach (MonoBehaviour component in FindSceneBehaviours(scene))
        {
            if (component == null || component.GetType().Name != "PlayerControllerTL6") continue;
            demo.player = component.transform;
            break;
        }

        GameObject streetObject = new GameObject("Generated Street View");
        streetObject.transform.SetParent(demoObject.transform, false);
        StreetView streetView = streetObject.AddComponent<StreetView>();
        streetView.settings = settings;
        streetView.art = art;
        streetView.setupCamera = false;
        streetView.loadRadiusChunks = 2;
        demo.streetView = streetView;

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }
        camera.orthographic = true;
        camera.orthographicSize = StreetView.DefaultOrthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.045f, 0.105f);
        return demo;
    }

    static GameObject[] FindEnemyPrefabs()
    {
        return new[]
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Animated.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Flying/Level1_Flying.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Turret/Level1_Turret.prefab"),
            null, // Aim=3 has no assigned prefab in this demo.
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/TVClaw.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/GrenadeBerserker.prefab")
        };
    }

    static MonoBehaviour[] FindSceneBehaviours(Scene scene)
    {
#if UNITY_2023_1_OR_NEWER
        MonoBehaviour[] all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
#else
        MonoBehaviour[] all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
        var found = new List<MonoBehaviour>();
        foreach (MonoBehaviour component in all)
            if (component != null && component.gameObject.scene == scene) found.Add(component);
        return found.ToArray();
    }

    static void ConfigureBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene scene in scenes)
            if (scene.path == scenePath) return;
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
}
