using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.GameStates;

/// <summary>
/// PauseState and GameOverState both freeze gameplay and restore it on exit, so the same tests run against each.
/// Each subclass below supplies the state; NUnit runs every inherited test once per subclass.
/// </summary>
public abstract class FrozenStateTests
{
    private float originalTimeScale;
    private CursorLockMode originalCursorLockMode;
    private bool originalCursorVisible;
    private GameState state;

    protected abstract GameState CreateState();

    [SetUp]
    public void SetUp()
    {
        originalTimeScale = Time.timeScale;
        originalCursorLockMode = Cursor.lockState;
        originalCursorVisible = Cursor.visible;

        Time.timeScale = 1f;
        state = CreateState();
    }

    // Time and Cursor are global Unity settings, so every test must leave them as it found them.
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = originalTimeScale;
        Cursor.lockState = originalCursorLockMode;
        Cursor.visible = originalCursorVisible;
    }

    [Test]
    public void Enter_FreezesGameplayTime()
    {
        state.Enter();

        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void Enter_UnlocksAndShowsCursor()
    {
        state.Enter();

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void Exit_RestoresTimeScaleFromBeforeEnter()
    {
        Time.timeScale = 0.5f;

        state.Enter();
        state.Exit();

        Assert.AreEqual(0.5f, Time.timeScale);
    }

    [Test]
    public void Exit_FollowedByPlayingStateEnter_TimeRuns()
    {
        state.Enter();
        state.Exit();
        new PlayingState().Enter();

        Assert.AreEqual(1f, Time.timeScale);
    }
}

public class PauseStateTests : FrozenStateTests
{
    protected override GameState CreateState() => new PauseState();
}

public class GameOverStateTests : FrozenStateTests
{
    protected override GameState CreateState() => new GameOverState();
}

public class PlayingStateTests
{
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
    }

    [Test]
    public void Enter_FromFrozenTime_RunsAtNormalSpeed()
    {
        // A restart enters Playing straight from a frozen scene reload, so Enter must set 1 rather than restore.
        Time.timeScale = 0f;

        new PlayingState().Enter();

        Assert.AreEqual(1f, Time.timeScale);
    }
}
