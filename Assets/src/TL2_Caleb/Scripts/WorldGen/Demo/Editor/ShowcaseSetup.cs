using EscapeThe90s.WorldGen;
using EscapeThe90s.WorldGen.Demo;
using UnityEditor;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Demo.Editor
{
// DEMO ONLY (see DemoWalker).
public static class ShowcaseSetup
{
    [MenuItem("Escape the 90s/Create Showcase Scene Objects")]
    static void Create()
    {
        var art = FindFirst<WorldArt>();
        if (art == null || art.spriteMaterial == null)
        {
            EditorUtility.DisplayDialog("Showcase", "Create a World Art asset first (Escape the 90s > Create World Art From Sprites).", "OK");
            return;
        }
        var settings = FindFirst<WorldGenSettings>();
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<WorldGenSettings>();
            AssetDatabase.CreateAsset(settings, AssetDatabase.GenerateUniqueAssetPath("Assets/WorldGenSettings.asset"));
            AssetDatabase.SaveAssets();
        }

        // Reuse a StreetView already in the scene (e.g. the one from the first test) instead of making a second street.
#if UNITY_2023_1_OR_NEWER
        StreetView street = UnityEngine.Object.FindAnyObjectByType<StreetView>();
#else
        StreetView street = UnityEngine.Object.FindObjectOfType<StreetView>();
#endif
        if (street == null)
        {
            var streetGo = new GameObject("Street");
            Undo.RegisterCreatedObjectUndo(streetGo, "Create Showcase");
            street = streetGo.AddComponent<StreetView>();
        }
        street.settings = settings;
        street.art = art;
        street.setupCamera = true;

        var player = new GameObject("Demo Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Showcase");
        player.AddComponent<DemoWalker>();
        player.transform.position = new Vector3(WorldConstants.ChunkWidthCells * 0.5f, 3f, 0f);
        street.follow = player.transform;

        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(camGo, "Create Showcase");
            cam = camGo.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = StreetView.DefaultOrthographicSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.05f, 0.13f);
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;
        follow.street = street;

        Selection.activeGameObject = street.gameObject;
        Debug.Log("Showcase ready. Set the Game view to a fixed 640x360 resolution, press Play, move with A/D or arrows, jump with Space.");
    }

    static T FindFirst<T>() where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}
}
