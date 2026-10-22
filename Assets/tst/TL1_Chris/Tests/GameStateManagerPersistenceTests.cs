using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeThe90s;
using EscapeThe90s.GameStates;

public class GameStateManagerPersistenceTests
{
    private Scene _firstScene;
    private Scene _secondScene;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Build the manager in its own scene so unloading that scene is a real scene change.
        _firstScene = SceneManager.CreateScene("GSM_FirstScene");
        SceneManager.SetActiveScene(_firstScene);

        new GameObject("GameStateManager").AddComponent<GameStateManager>();
        yield return null;   // let Start() run so the manager enters MenuState
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (GameStateManager.Instance != null)
        {
            Object.Destroy(GameStateManager.Instance.gameObject);
        }
        Time.timeScale = 1f;

        // Unload the scenes this test created so the next test can reuse the same names.
        if (_firstScene.isLoaded) yield return SceneManager.UnloadSceneAsync(_firstScene);
        if (_secondScene.isLoaded) yield return SceneManager.UnloadSceneAsync(_secondScene);
        yield return null;
    }

    // Creates a new scene, makes it active, and unloads the scene the manager was created in.
    private IEnumerator ChangeScene(string sceneName)
    {
        _secondScene = SceneManager.CreateScene(sceneName);
        SceneManager.SetActiveScene(_secondScene);
        yield return SceneManager.UnloadSceneAsync(_firstScene);
    }

    [UnityTest]
    public IEnumerator Manager_SurvivesSceneChange()
    {
        GameStateManager before = GameStateManager.Instance;

        yield return ChangeScene("GSM_SecondScene");

        Assert.IsTrue(GameStateManager.Instance != null, "Manager was destroyed by the scene change.");
        Assert.AreSame(before, GameStateManager.Instance);
        Assert.AreEqual("DontDestroyOnLoad", GameStateManager.Instance.gameObject.scene.name);
    }

    [UnityTest]
    public IEnumerator CurrentState_IsSameInstanceAfterSceneChange()
    {
        GameStateManager.Instance.RequestStart();
        GameState stateBefore = GameStateManager.Instance.CurrentState;
        Assert.IsInstanceOf<PlayingState>(stateBefore);

        yield return ChangeScene("GSM_SecondScene");

        Assert.AreSame(stateBefore, GameStateManager.Instance.CurrentState);
    }

    [UnityTest]
    public IEnumerator DuplicateManager_InNewScene_IsDestroyed_AndStateIsKept()
    {
        GameStateManager original = GameStateManager.Instance;
        original.RequestStart();
        GameState stateBefore = original.CurrentState;

        yield return ChangeScene("GSM_SecondScene");

        // Simulates the next scene also having a GameStateManager placed in it.
        GameStateManager duplicate = new GameObject("DuplicateGSM").AddComponent<GameStateManager>();
        yield return null;   // Destroy() takes effect at the end of the frame

        Assert.IsTrue(duplicate == null, "Duplicate manager was not destroyed.");
        Assert.AreSame(original, GameStateManager.Instance);
        Assert.AreSame(stateBefore, GameStateManager.Instance.CurrentState);
        Assert.AreEqual(1, Object.FindObjectsByType<GameStateManager>().Length);
    }
}
