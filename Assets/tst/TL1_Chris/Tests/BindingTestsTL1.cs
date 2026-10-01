using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.TestTools;
using EscapeThe90s.GameStates;

public class BindingTestsTL1
{
    // A Test behaves as an ordinary method
    [Test]
    public void BaseRef_ToSubclass_UsesOverride()
    {
        GameState baseline = new GameState();
        GameState actual = new MenuState();
        string a = baseline.Enter();
        string b = actual.Enter();
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void BaseRef_ToSubclass_NoBindingChange()
    {
        GameState baseline = new GameState();
        GameState actual = new GameState();

        Assert.AreEqual(baseline.Enter(), actual.Enter());
    }
}
