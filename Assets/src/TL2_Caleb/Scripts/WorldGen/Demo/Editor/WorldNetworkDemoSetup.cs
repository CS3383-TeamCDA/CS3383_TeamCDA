using EscapeThe90s.WorldGen;
using EscapeThe90s.WorldGen.Demo;
using UnityEditor;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Demo.Editor
{
public static class WorldNetworkDemoSetup
{
    [MenuItem("Escape the 90s/Create World Network Demo")]
    static void Create()
    {
        WorldArt art = FindAsset<WorldArt>();
        if (art == null || art.spriteMaterial == null || art.groundSheet == null)
        {
            EditorUtility.DisplayDialog("World Network Demo", "Create a World Art asset first with Escape the 90s > Create World Art From Sprites, then run this menu item again.", "OK");
            return;
        }

        WorldGenSettings settings = FindAsset<WorldGenSettings>();
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<WorldGenSettings>();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/WorldGenSettings.asset");
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
        }

        var demoGo = new GameObject("World Network Demo");
        Undo.RegisterCreatedObjectUndo(demoGo, "Create World Network Demo");
        var demo = demoGo.AddComponent<WorldNetworkDemo>();
        demo.settings = settings;
        demo.art = art;
        demo.playerPrefab = FindPlayerPrefab();
        demo.enemyPrefabsByKind = FindEnemyPrefabs();
        demo.streetView = FindSceneObject<StreetView>();
        if (demo.playerPrefab == null)
        {
            DemoWalker existingPlayer = FindSceneObject<DemoWalker>();
            if (existingPlayer != null) demo.player = existingPlayer.transform;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            var cameraGo = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(cameraGo, "Create World Network Demo Camera");
            cameraGo.tag = "MainCamera";
            camera = cameraGo.AddComponent<Camera>();
        }
        camera.orthographic = true;
        camera.orthographicSize = 16f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.045f, 0.105f);

        Selection.activeGameObject = demoGo;
        Debug.Log("Playable World Network Demo is ready. Press Play, walk with A/D and jump with Space, then press Enter at a street end to choose the next intersection exit.");
    }

    static T FindAsset<T>() where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static T FindSceneObject<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindAnyObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }

    static GameObject FindPlayerPrefab()
    {
        string[] guids = AssetDatabase.FindAssets("PlayerTL6 t:Prefab", new[] { "Assets/prefabs/TL6_Matteo" });
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static GameObject[] FindEnemyPrefabs()
    {
        return new[]
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Animated.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Flying/Level1_Flying.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Turret/Level1_Turret.prefab")
        };
    }
}
}
