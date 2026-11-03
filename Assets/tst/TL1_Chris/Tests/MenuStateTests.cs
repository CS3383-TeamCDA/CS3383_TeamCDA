using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.GameStates;

public class MenuStateTests
{
    #region Attributes

    private float originalTimeScale;
    private CursorLockMode originalCursorLockMode;
    private bool originalCursorVisible;
    private MenuState menuState;

    #endregion

    #region Setup

    [SetUp]
    public void SetUp()
    {
        originalTimeScale = Time.timeScale;
        originalCursorLockMode = Cursor.lockState;
        originalCursorVisible = Cursor.visible;

        Time.timeScale = 1f;
        menuState = new MenuState();
    }

    // Time and Cursor are global Unity settings, so every test must leave them as it found them.
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = originalTimeScale;
        Cursor.lockState = originalCursorLockMode;
        Cursor.visible = originalCursorVisible;
    }

    #endregion

    #region Enter

    [Test]
    public void Enter_FreezesGameplayTime()
    {
        menuState.Enter();

        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void Enter_UnlocksAndShowsCursor()
    {
        menuState.Enter();

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void Enter_ThroughGameStateReference_RunsMenuStateOverride()
    {
        GameState state = menuState;

        state.Enter();

        Assert.AreEqual(0f, Time.timeScale, "Base GameState.Enter() ran instead of the MenuState override.");
    }

    #endregion

    #region Update

    [Test]
    public void Update_UnlocksCursorIfAnotherSystemLocksIt()
    {
        menuState.Enter();
        Cursor.lockState = CursorLockMode.Locked;

        // Without a focused Game view (e.g. batch mode), Unity ignores cursor locking, so this test cannot apply.
        Assume.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked), "Cursor locking is unavailable in this test environment.");

        menuState.Update();

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
    }

    [Test]
    public void Update_DoesNotChangeTimeScale()
    {
        menuState.Enter();

        menuState.Update();

        Assert.AreEqual(0f, Time.timeScale);
    }

    #endregion

    #region Exit

    [Test]
    public void Exit_RestoresTimeScaleFromBeforeEnter()
    {
        menuState.Enter();
        menuState.Exit();

        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void Exit_AfterEnteringFromPause_RestoresPausedTimeScale()
    {
        Time.timeScale = 0f;

        menuState.Enter();
        menuState.Exit();

        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void Exit_FollowedByPlayingStateEnter_UnfreezesGameplay()
    {
        PlayingState playingState = new PlayingState();
        Time.timeScale = 0f;

        menuState.Enter();
        menuState.Exit();
        playingState.Enter();

        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void Exit_WithoutEnter_RestoresSafeDefaults()
    {
        Time.timeScale = 0f;

        menuState.Exit();

        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    #endregion
}
