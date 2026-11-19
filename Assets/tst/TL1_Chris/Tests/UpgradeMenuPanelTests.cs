using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.HUD;

/// <summary>
/// Checks that the upgrade menu opens over the panel that opened it and returns to it on Back.
/// </summary>
public class UpgradeMenuPanelTests
{
    private UpgradeMenuPanel upgradeMenu;
    private GameOverPanel gameOverPanel;

    [SetUp]
    public void SetUp()
    {
        upgradeMenu = new GameObject("UpgradeMenu").AddComponent<UpgradeMenuPanel>();
        gameOverPanel = new GameObject("GameOverMenu").AddComponent<GameOverPanel>();
        upgradeMenu.Hide();
        gameOverPanel.Show();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(upgradeMenu.gameObject);
        Object.DestroyImmediate(gameOverPanel.gameObject);
    }

    [Test]
    public void Open_ShowsMenu_AndHidesOpener()
    {
        upgradeMenu.Open(gameOverPanel);

        Assert.IsTrue(upgradeMenu.gameObject.activeSelf);
        Assert.IsFalse(gameOverPanel.gameObject.activeSelf);
    }

    [Test]
    public void Back_HidesMenu_AndShowsOpenerAgain()
    {
        upgradeMenu.Open(gameOverPanel);

        upgradeMenu.Back();

        Assert.IsFalse(upgradeMenu.gameObject.activeSelf);
        Assert.IsTrue(gameOverPanel.gameObject.activeSelf);
    }

    [Test]
    public void Close_HidesMenu_WithoutShowingOpener()
    {
        // Used when the game state changes underneath the menu; the new state decides what is shown.
        upgradeMenu.Open(gameOverPanel);

        upgradeMenu.Close();

        Assert.IsFalse(upgradeMenu.gameObject.activeSelf);
        Assert.IsFalse(gameOverPanel.gameObject.activeSelf);
    }

    [Test]
    public void OpenUpgrades_FromGameOverPanel_FindsMenuInScene()
    {
        gameOverPanel.OpenUpgrades();

        Assert.IsTrue(upgradeMenu.gameObject.activeSelf);
        Assert.IsFalse(gameOverPanel.gameObject.activeSelf);
    }

    [Test]
    public void OpenUpgrades_FromMainMenu_BackReturnsToMainMenu()
    {
        MainMenuPanel mainMenu = new GameObject("MainMenu").AddComponent<MainMenuPanel>();

        mainMenu.OpenUpgrades();
        Assert.IsTrue(upgradeMenu.gameObject.activeSelf);
        Assert.IsFalse(mainMenu.gameObject.activeSelf);

        upgradeMenu.Back();
        Assert.IsTrue(mainMenu.gameObject.activeSelf, "Back should return to the panel that opened the menu.");

        Object.DestroyImmediate(mainMenu.gameObject);
    }
}
