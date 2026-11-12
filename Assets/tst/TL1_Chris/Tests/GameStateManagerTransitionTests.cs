using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeThe90s;
using EscapeThe90s.GameStates;

/// <summary>
/// Checks which Request* calls GameStateManager accepts in each state, and that rejected requests leave the state alone.
/// </summary>
public class GameStateManagerTransitionTests
{
    private GameStateManager manager;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        manager = new GameObject("GameStateManager").AddComponent<GameStateManager>();
        yield return null;   // let Start() run so the manager enters MenuState
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (manager != null)
        {
            Object.Destroy(manager.gameObject);
        }
        Time.timeScale = 1f;
        yield return null;
    }

    #region Helpers

    private void GoToPlaying()
    {
        manager.RequestStart();
        Assert.IsInstanceOf<PlayingState>(manager.CurrentState, "Setup failed: could not reach PlayingState.");
    }

    private void GoToPaused()
    {
        GoToPlaying();
        manager.RequestPause();
        Assert.IsInstanceOf<PauseState>(manager.CurrentState, "Setup failed: could not reach PauseState.");
    }

    private void GoToGameOver()
    {
        GoToPlaying();
        manager.RequestGameOver();
        Assert.IsInstanceOf<GameOverState>(manager.CurrentState, "Setup failed: could not reach GameOverState.");
    }

    // Calls the request and checks the manager kept the exact same state object.
    private void AssertRequestIgnored(System.Action request)
    {
        GameState before = manager.CurrentState;
        request();
        Assert.AreSame(before, manager.CurrentState);
    }

    #endregion

    #region Startup

    [Test]
    public void Start_EntersMenuState()
    {
        Assert.IsInstanceOf<MenuState>(manager.CurrentState);
    }

    #endregion

    #region Valid transitions

    [Test]
    public void RequestStart_FromMenu_EntersPlaying()
    {
        manager.RequestStart();

        Assert.IsInstanceOf<PlayingState>(manager.CurrentState);
    }

    [Test]
    public void RequestPause_FromPlaying_EntersPause()
    {
        GoToPlaying();

        manager.RequestPause();

        Assert.IsInstanceOf<PauseState>(manager.CurrentState);
    }

    [Test]
    public void RequestResume_FromPause_ReturnsToSamePlayingStateInstance()
    {
        GoToPlaying();
        GameState playing = manager.CurrentState;
        manager.RequestPause();

        manager.RequestResume();

        Assert.AreSame(playing, manager.CurrentState, "Resume should continue the same run, not create a new PlayingState.");
    }

    [Test]
    public void RequestGameOver_FromPlaying_EntersGameOver()
    {
        GoToPlaying();

        manager.RequestGameOver();

        Assert.IsInstanceOf<GameOverState>(manager.CurrentState);
    }

    [Test]
    public void RequestReturnToMenu_FromPlaying_EntersMenu()
    {
        GoToPlaying();

        manager.RequestReturnToMenu();

        Assert.IsInstanceOf<MenuState>(manager.CurrentState);
    }

    [Test]
    public void RequestReturnToMenu_FromPause_EntersMenu()
    {
        GoToPaused();

        manager.RequestReturnToMenu();

        Assert.IsInstanceOf<MenuState>(manager.CurrentState);
    }

    [Test]
    public void RequestReturnToMenu_FromGameOver_EntersMenu()
    {
        GoToGameOver();

        manager.RequestReturnToMenu();

        Assert.IsInstanceOf<MenuState>(manager.CurrentState);
    }

    [Test]
    public void PauseThenResume_TimeRunsAgain()
    {
        GoToPlaying();

        manager.RequestPause();
        Assert.AreEqual(0f, Time.timeScale, "Pause should freeze gameplay.");

        manager.RequestResume();
        Assert.AreEqual(1f, Time.timeScale);
    }

    #endregion

    #region Rejected transitions

    [Test]
    public void RequestPause_FromMenu_IsIgnored()
    {
        AssertRequestIgnored(manager.RequestPause);
    }

    [Test]
    public void RequestStart_FromPlaying_IsIgnored()
    {
        GoToPlaying();
        AssertRequestIgnored(manager.RequestStart);
    }

    [Test]
    public void RequestResume_FromPlaying_IsIgnored()
    {
        GoToPlaying();
        AssertRequestIgnored(manager.RequestResume);
    }

    [Test]
    public void RequestGameOver_FromPause_IsIgnored()
    {
        GoToPaused();
        AssertRequestIgnored(manager.RequestGameOver);
    }

    [Test]
    public void RequestRestart_FromMenu_IsIgnored()
    {
        AssertRequestIgnored(manager.RequestRestart);
    }

    [Test]
    public void RequestRestart_FromPlaying_IsIgnored()
    {
        GoToPlaying();
        AssertRequestIgnored(manager.RequestRestart);
    }

    #endregion
}
