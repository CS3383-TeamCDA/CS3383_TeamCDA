using NUnit.Framework;
using TMPro;
using UnityEngine;
using EscapeThe90s.HUD;

/// <summary>
/// Checks that HUDController keeps the HUD's values in range. Which panel is shown per state is tested in UIPanelControllerTests.
/// </summary>
public class HUDControllerTests
{
    private GameObject hudObject;
    private HUDController hud;
    private HUDPanel hudPanel;
    private TMP_Text healthText;
    private TMP_Text scoreText;

    [SetUp]
    public void SetUp()
    {
        hudPanel = new GameObject("HUDPanel").AddComponent<HUDPanel>();
        healthText = CreateText("HealthText", hudPanel.transform);
        scoreText = CreateText("ScoreText", hudPanel.transform);
        TL1TestUtil.SetField(hudPanel, "healthText", healthText);
        TL1TestUtil.SetField(hudPanel, "scoreText", scoreText);

        // Starts inactive so the field is set before Awake runs (Awake already writes to hudPanel),
        // just like a prefab wired in the Inspector.
        hudObject = new GameObject("HUD");
        hudObject.SetActive(false);
        hud = hudObject.AddComponent<HUDController>();
        TL1TestUtil.SetField(hud, "hudPanel", hudPanel);
        hudObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(hudObject);
        Object.DestroyImmediate(hudPanel.gameObject);
    }

    private static TMP_Text CreateText(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.AddComponent<TextMeshProUGUI>();
    }

    [Test]
    public void UpdateHealth_Negative_DisplaysZero()
    {
        hud.UpdateHealth(-10);

        Assert.AreEqual("Health: 0", healthText.text);
    }

    [Test]
    public void UpdateScore_Negative_DisplaysZero()
    {
        hud.UpdateScore(-5);

        Assert.AreEqual("Score: 0", scoreText.text);
    }
}
