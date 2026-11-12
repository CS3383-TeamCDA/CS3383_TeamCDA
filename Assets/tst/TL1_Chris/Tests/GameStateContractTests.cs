using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeThe90s;
using EscapeThe90s.Contracts;

/// <summary>
/// Checks GameStateManager through the IGameState contract that other features use.
/// </summary>
public class GameStateContractTests
{
    private GameStateManager manager;
    private IGameState gameState;
    private List<GameStateChange> changes;

    [SetUp]
    public void SetUp()
    {
        // No yield here: Awake has run but Start has not, so tests can see the state before the first transition.
        manager = new GameObject("GameStateManager").AddComponent<GameStateManager>();
        gameState = manager;
        changes = new List<GameStateChange>();
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

    // Waits for Start() (enters Menu), then records every change from here on.
    private IEnumerator StartAndRecord()
    {
        yield return null;
        gameState.OnStateTypeChanged += changes.Add;
    }

    [Test]
    public void CurrentStateType_BeforeStart_IsNone()
    {
        Assert.AreEqual(GameStateType.None, gameState.CurrentStateType);
    }

    [UnityTest]
    public IEnumerator CurrentStateType_AfterStart_IsMenu()
    {
        yield return null;

        Assert.AreEqual(GameStateType.Menu, gameState.CurrentStateType);
    }

    [UnityTest]
    public IEnumerator OnStateTypeChanged_FiresOnce_WithPreviousAndCurrent()
    {
        yield return StartAndRecord();

        gameState.RequestStart();

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(GameStateType.Menu, changes[0].Previous);
        Assert.AreEqual(GameStateType.Playing, changes[0].Current);
    }

    [UnityTest]
    public IEnumerator OnStateTypeChanged_DoesNotFire_ForRejectedRequest()
    {
        yield return StartAndRecord();

        gameState.RequestPause();      // not allowed from Menu
        gameState.RequestResume();     // not allowed from Menu
        gameState.RequestGameOver();   // not allowed from Menu

        Assert.AreEqual(0, changes.Count);
    }

    [UnityTest]
    public IEnumerator IsNewRun_StartingFromMenu_IsTrue()
    {
        yield return StartAndRecord();

        gameState.RequestStart();

        Assert.IsTrue(changes[0].IsNewRun);
    }

    [UnityTest]
    public IEnumerator IsNewRun_ResumingFromPause_IsFalse()
    {
        yield return StartAndRecord();
        gameState.RequestStart();
        gameState.RequestPause();

        gameState.RequestResume();

        GameStateChange resume = changes[changes.Count - 1];
        Assert.AreEqual(GameStateType.Paused, resume.Previous);
        Assert.AreEqual(GameStateType.Playing, resume.Current);
        Assert.IsFalse(resume.IsNewRun, "Resuming continues the current run; it must not be reported as a new one.");
    }

    [UnityTest]
    public IEnumerator IsNewRun_ForNonPlayingStates_IsFalse()
    {
        yield return StartAndRecord();
        gameState.RequestStart();

        gameState.RequestPause();
        gameState.RequestReturnToMenu();

        Assert.IsFalse(changes[1].IsNewRun, "Entering Pause is not a new run.");
        Assert.IsFalse(changes[2].IsNewRun, "Entering Menu is not a new run.");
    }

    [UnityTest]
    public IEnumerator Instance_AfterManagerDestroyed_IsNull()
    {
        yield return null;
        Assert.AreSame(manager, GameStateManager.Instance);

        Object.Destroy(manager.gameObject);
        yield return null;

        // Checked with ReferenceEquals: Unity's == would also report a destroyed-but-still-referenced object as null.
        Assert.IsTrue(ReferenceEquals(GameStateManager.Instance, null), "OnDestroy should clear the static Instance.");
    }
}
