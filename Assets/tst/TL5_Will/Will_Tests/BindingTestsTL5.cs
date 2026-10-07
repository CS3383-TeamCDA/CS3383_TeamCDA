using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BindingTestsTL5
{
    [Test]
    public void BaseRef_ToSubclass_UsesOverride(){
        UpgradeEffect baseline = new UpgradeEffect();
        UpgradeEffect actual = new HealthEffect();
        string a = baseline.Apply();
        string b = actual.Apply();

        Assert.AreNotEqual(a, b); // assert: DIFFERENT​
    }

    [Test]
    public void BaseRef_ToBase_NoBindingChange()
    {
        UpgradeEffect baseline = new UpgradeEffect(); 
        UpgradeEffect actual = new UpgradeEffect();
        //Assert.AreNotEqual(baseline.Apply(), actual.Apply());
        Assert.AreEqual(baseline.Apply(), actual.Apply());
        // Run 1: RED - same output, nothing to bind to
        // Run 2: change to Assert.AreEqual -> GREEN -> commit
    }
}
