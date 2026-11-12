#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using EscapeThe90s;
using EscapeThe90s.Contracts;
using EscapeThe90s.HUD;
using EscapeThe90s.WorldGen;
using EscapeThe90s.WorldGen.Integration;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class EnemySceneIntegrationTests
{
    [Test]
    public void SpawningSandboxIncludesMenuAndActiveHudController()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/TL4_Tyler/EnemySpawningMvp.unity");
        try
        {
            var roots = scene.GetRootGameObjects();
            Assert.AreEqual(1, roots.SelectMany(r => r.GetComponentsInChildren<MainMenuPanel>(true)).Count());
            var hud = roots.SelectMany(r => r.GetComponentsInChildren<HUDController>(true)).Single();
            Assert.IsTrue(hud.isActiveAndEnabled, "HUD must observe Playing and hide the menu.");
            var world = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                .Single(c => c != null && c.GetType().Name == "WorldNetworkDemo");
            Assert.IsTrue(world.isActiveAndEnabled, "A menu and HUD alone do not make a playable scene.");
            Assert.IsNotNull(world.GetType().GetField("settings").GetValue(world));
            Assert.IsNotNull(world.GetType().GetField("art").GetValue(world));
            Assert.IsNotNull(world.GetType().GetField("playerPrefab").GetValue(world));
            Assert.IsNotNull(world.GetType().GetField("streetView").GetValue(world));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void GeneratedDemoUsesNewPrefabsInItsActiveSpawnerPath()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/GeneratedWorldGameDemo.unity");
        try
        {
            var demo = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                .Single(c => c != null && c.GetType().Name == "WorldNetworkDemo");
            var prefabs = (GameObject[])demo.GetType().GetField("enemyPrefabsByKind").GetValue(demo);
            Assert.IsTrue(demo.isActiveAndEnabled);
            Assert.AreEqual(EnemyType.TVClaw, prefabs[4].GetComponent<EnemyBase>().Type);
            Assert.AreEqual(EnemyType.GrenadeBerserker, prefabs[5].GetComponent<EnemyBase>().Type);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void GeneratedStreetCyclesAllConfiguredKindsAndSkipsEmptyAimSlot()
    {
        var go = new GameObject("Generated content test");
        try
        {
            var content = go.AddComponent<GeneratedStreetContent>();
            var prefabs = new GameObject[] { go, go, go, null, go, go };
            typeof(GeneratedStreetContent).GetField("_enemyPrefabs", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(content, prefabs);
            var layout = new StreetLayout { Chunks = new[] {
                new ChunkData { Enemies = new EnemySlot[3] },
                new ChunkData { Enemies = new EnemySlot[7] }
            }};
            var assign = typeof(GeneratedStreetContent).GetMethod("EnsureAllEnemyKinds", BindingFlags.NonPublic | BindingFlags.Instance);
            assign.Invoke(content, new object[] { layout });
            int[] actual = layout.Chunks.SelectMany(c => c.Enemies).Select(e => e.KindIndex).ToArray();
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 4, 5, 0, 1, 2, 4, 5 }, actual);
            assign.Invoke(content, new object[] { layout });
            CollectionAssert.AreEqual(actual, layout.Chunks.SelectMany(c => c.Enemies).Select(e => e.KindIndex).ToArray());
        }
        finally { Object.DestroyImmediate(go); }
    }

    [UnityTest]
    public IEnumerator StartButtonEntersPlayingAndHudHidesMenu()
    {
        var managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL1_Chris/GameStateManager.prefab");
        var manager = Object.Instantiate(managerPrefab);
        var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL1_Chris/MainMenu.prefab"));
        var hud = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL1_Chris/HUD.prefab"));
        try
        {
            yield return null;
            Assert.AreEqual(GameStateType.Menu, GameStateManager.Instance.CurrentStateType);
            menu.GetComponentsInChildren<Button>(true).Single(b => b.name == "StartButton").onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameStateType.Playing, GameStateManager.Instance.CurrentStateType);
            Assert.IsFalse(menu.GetComponentInChildren<MainMenuPanel>(true).gameObject.activeSelf);
            Assert.AreEqual(1f, Time.timeScale);
        }
        finally
        {
            Object.Destroy(menu);
            Object.Destroy(hud);
            Object.Destroy(manager);
            Time.timeScale = 1;
        }
        yield return null;
    }
}
#endif
