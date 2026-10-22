using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.GameStates;

public class BindingTestsTL1
{
    // Test-only state that does NOT override Enter(), so the base GameState version runs.
    private class NoOverrideState : GameState { }

    [SetUp]
    public void SetUp()
    {
        // Start from a value no state sets, so any change is clearly caused by Enter().
        Time.timeScale = 0.5f;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
    }

    [Test]
    public void BaseRef_ToSubclass_UsesOverride()
    {
        GameState state = new PauseState();
        state.Enter();
        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void BaseRef_ToSubclass_NoBindingChange()
    {
        GameState state = new NoOverrideState();
        state.Enter();
        Assert.AreEqual(0.5f, Time.timeScale);
    }
}
