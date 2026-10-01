using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BindingTestsTL3
{
    // A Test behaves as an ordinary method
    [Test]
    public void BaseRef_ToSubclass_UsesOverride()
    {
        PowerUpEffect baseline = new PowerUpEffect();
        PowerUpEffect actual = new SpeedBoostEffect();
        string a = baseline.Apply();
        string b = actual.Apply();
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void BaseRef_ToSubclass_NoBindingChange()
    {
        PowerUpEffect baseline = new PowerUpEffect();
        PowerUpEffect actual = new PowerUpEffect();
        Assert.AreEqual(baseline.Apply(), actual.Apply());
    }
}
