using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using static UnityEditor.Progress;

public class BindingTestsMatteo
{
    [Test]
    public void BaseRef_ToSubclass_UsesOverride()
    {
        MovementState baseline = new MovementState();
        MovementState actual = new RunningState();
        string a = baseline.Execute();
        string b = actual.Execute();

        Assert.AreNotEqual(a, b); // assert: DIFFERENT​
    }

    [Test]
    public void BaseRef_ToBase_NoBindingChange()
    {
        MovementState baseline = new MovementState();
        MovementState actual = new MovementState();
        //Assert.AreNotEqual(baseline.Execute(), actual.Execute());
        Assert.AreEqual(baseline.Execute(), actual.Execute());
        // Run 1: RED - same output, nothing to bind to
        // Run 2: change to Assert.AreEqual -> GREEN -> commit
    }
}
