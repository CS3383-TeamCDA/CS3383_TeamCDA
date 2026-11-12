using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeThe90s;
using EscapeThe90s.HUD;

/// <summary>
/// Checks HUDController's value clamping and which panel it shows for each game state.
/// </summary>
public class HUDControllerTests
{
    private GameObject hudObject;
    private HUDController hud;
    private HUDPanel hudPanel;
    private MainMenuPanel mainMenuPanel;
    private PausePanel pausePanel;
    private TMP_Text healthText;
    private TMP_Text scoreText;
    private GameStateManager manager;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (Object obj in new Object[] { hudObject, hudPanel, mainMenuPanel, pausePanel, manager })
        {
            if (obj != null)
            {
                Object.Destroy(obj is Component c ? c.gameObject : obj);
            }
        }
        Time.timeScale = 1f;
        yield return null;
    }

    // Builds a HUD with its panels assigned. The HUD object starts inactive so the fields are set
    // before Awake runs (Awake already writes to hudPanel), just like a prefab wired in the Inspector.
    private void CreateHUD()
    {
        hudPanel = new GameObject("HUDPanel").AddComponent<HUDPanel>();
        healthText = CreateText("HealthText", hudPanel.transform);
        scoreText = CreateText("ScoreText", hudPanel.transform);
        TL1TestUtil.SetField(hudPanel, "healthText", healthText);
        TL1TestUtil.SetField(hudPanel, "scoreText", scoreText);

        mainMenuPanel = new GameObject("MainMenuPanel").AddComponent<MainMenuPanel>();
        pausePanel = new GameObject("PausePanel").AddComponent<PausePanel>();

        hudObject = new GameObject("HUD");
        hudObject.SetActive(false);
        hud = hudObject.AddComponent<HUDController>();
        TL1TestUtil.SetField(hud, "hudPanel", hudPanel);
        TL1TestUtil.SetField(hud, "mainMenuPanel", mainMenuPanel);
        TL1TestUtil.SetField(hud, "pausePanel", pausePanel);
        hudObject.SetActive(true);
    }

    private static TMP_Text CreateText(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.AddComponent<TextMeshProUGUI>();
    }

    private IEnumerator CreateManagerInMenu()
    {
        manager = new GameObject("GameStateManager").AddComponent<GameStateManager>();
        yield return null;   // Start() enters MenuState
    }

    private void AssertOnlyVisible(Component expected)
    {
        Assert.AreEqual(expected == mainMenuPanel, mainMenuPanel.gameObject.activeSelf, "Main menu panel visibility is wrong.");
        Assert.AreEqual(expected == hudPanel, hudPanel.gameObject.activeSelf, "HUD panel visibility is wrong.");
        Assert.AreEqual(expected == pausePanel, pausePanel.gameObject.activeSelf, "Pause panel visibility is wrong.");
    }

    #region Value clamping

    [Test]
    public void UpdateHealth_Negative_DisplaysZero()
    {
        CreateHUD();

        hud.UpdateHealth(-10);

        Assert.AreEqual("Health: 0", healthText.text);
    }

    [Test]
    public void UpdateScore_Negative_DisplaysZero()
    {
        CreateHUD();

        hud.UpdateScore(-5);

        Assert.AreEqual("Score: 0", scoreText.text);
    }

    #endregion

    #region Panel per state

    [UnityTest]
    public IEnumerator MenuState_ShowsOnlyMainMenu()
    {
        yield return CreateManagerInMenu();

        CreateHUD();   // OnEnable shows the panel for the current state

        AssertOnlyVisible(mainMenuPanel);
    }

    [UnityTest]
    public IEnumerator PlayingState_ShowsOnlyHUD()
    {
        yield return CreateManagerInMenu();
        CreateHUD();

        manager.RequestStart();
        yield return null;   // HUDController picks up the change in Update

        AssertOnlyVisible(hudPanel);
    }

    [UnityTest]
    public IEnumerator PauseState_ShowsOnlyPausePanel()
    {
        yield return CreateManagerInMenu();
        CreateHUD();
        manager.RequestStart();
        yield return null;

        manager.RequestPause();
        yield return null;

        AssertOnlyVisible(pausePanel);
    }

    #endregion
}
