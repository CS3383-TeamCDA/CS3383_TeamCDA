using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeThe90s;
using EscapeThe90s.HUD;
using EscapeThe90s.UI;

/// <summary>
/// Checks that UIPanelController shows exactly one screen for each game state.
/// </summary>
public class UIPanelControllerTests
{
    private GameObject controllerObject;
    private HUDPanel hudPanel;
    private MainMenuPanel mainMenuPanel;
    private PausePanel pausePanel;
    private GameOverPanel gameOverPanel;
    private GameStateManager manager;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        manager = new GameObject("GameStateManager").AddComponent<GameStateManager>();
        yield return null;   // Start() enters MenuState
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (Object obj in new Object[] { controllerObject, hudPanel, mainMenuPanel, pausePanel, gameOverPanel, manager })
        {
            if (obj != null)
            {
                Object.Destroy(obj is Component c ? c.gameObject : obj);
            }
        }
        Time.timeScale = 1f;
        yield return null;
    }

    // Builds a controller with its panels assigned. It starts inactive so the fields are set before
    // Awake and OnEnable run, just like a prefab wired in the Inspector.
    private void CreateController()
    {
        hudPanel = new GameObject("HUDPanel").AddComponent<HUDPanel>();
        mainMenuPanel = new GameObject("MainMenuPanel").AddComponent<MainMenuPanel>();
        pausePanel = new GameObject("PausePanel").AddComponent<PausePanel>();
        gameOverPanel = new GameObject("GameOverPanel").AddComponent<GameOverPanel>();

        controllerObject = new GameObject("UIPanelController");
        controllerObject.SetActive(false);
        UIPanelController controller = controllerObject.AddComponent<UIPanelController>();
        TL1TestUtil.SetField(controller, "hudPanel", hudPanel);
        TL1TestUtil.SetField(controller, "mainMenuPanel", mainMenuPanel);
        TL1TestUtil.SetField(controller, "pausePanel", pausePanel);
        TL1TestUtil.SetField(controller, "gameOverPanel", gameOverPanel);
        controllerObject.SetActive(true);   // OnEnable shows the panel for the current state
    }

    private void AssertOnlyVisible(Component expected)
    {
        Assert.AreEqual(expected == mainMenuPanel, mainMenuPanel.gameObject.activeSelf, "Main menu panel visibility is wrong.");
        Assert.AreEqual(expected == hudPanel, hudPanel.gameObject.activeSelf, "HUD panel visibility is wrong.");
        Assert.AreEqual(expected == pausePanel, pausePanel.gameObject.activeSelf, "Pause panel visibility is wrong.");
        Assert.AreEqual(expected == gameOverPanel, gameOverPanel.gameObject.activeSelf, "Game over panel visibility is wrong.");
    }

    [Test]
    public void MenuState_ShowsOnlyMainMenu()
    {
        CreateController();

        AssertOnlyVisible(mainMenuPanel);
    }

    [UnityTest]
    public IEnumerator PlayingState_ShowsOnlyHUD()
    {
        CreateController();

        manager.RequestStart();
        yield return null;   // the controller picks up the change in Update

        AssertOnlyVisible(hudPanel);
    }

    [UnityTest]
    public IEnumerator PauseState_ShowsOnlyPausePanel()
    {
        CreateController();
        manager.RequestStart();
        yield return null;

        manager.RequestPause();
        yield return null;

        AssertOnlyVisible(pausePanel);
    }

    [UnityTest]
    public IEnumerator GameOverState_ShowsOnlyGameOverPanel()
    {
        CreateController();
        manager.RequestStart();
        yield return null;

        manager.RequestGameOver();
        yield return null;

        AssertOnlyVisible(gameOverPanel);
    }
}
