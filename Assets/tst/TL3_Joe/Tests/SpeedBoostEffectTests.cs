using System.Collections.Generic;
using NUnit.Framework;
using EscapeThe90s.PowerUps;

/// <summary>
/// Edit Mode tests for SpeedBoostEffect.Apply().
/// Checks the Champion doc contract: multiplier = 1.0 + 0.5 x stacks,
/// max 3 stacks (2.5x), value always in [1.0, 2.5], invalid input throws.
/// </summary>
public class SpeedBoostEffectTests
{
    private const float Tolerance = 0.0001f;

    /// <summary>
    /// Records every call so tests can check what Apply() sent to TL6.
    /// Kept private to this file so it cannot clash with a project-wide
    /// MockPowerUpTarget.
    /// </summary>
    private class RecordingTarget : IPowerUpTarget
    {
        public readonly List<float> SpeedMultipliersSent = new List<float>();
        public readonly List<int> HealAmountsSent = new List<int>();
        public readonly List<float> PickupRadiiSent = new List<float>();

        public float SpeedMultiplier { get; private set; } = 1.0f;
        public float PickupRadius { get; private set; } = 1.5f;

        // Not used by Speed Boost; present only to satisfy the interface.
        public UnityEngine.Vector3 Position { get; set; } = UnityEngine.Vector3.zero;

        public void SetSpeedMultiplier(float mult)
        {
            SpeedMultipliersSent.Add(mult);
            SpeedMultiplier = mult;
        }

        public void Heal(int amount)
        {
            HealAmountsSent.Add(amount);
        }

        public void SetPickupRadius(float radius)
        {
            PickupRadiiSent.Add(radius);
            PickupRadius = radius;
        }
    }

    /// <summary>
    /// Test-only subclass. MaxStacks has a protected setter, so only a
    /// derived class can set it. This does not override Apply(), so the
    /// real SpeedBoostEffect.Apply() is what runs in every test.
    /// </summary>
    private class TestableSpeedBoostEffect : SpeedBoostEffect
    {
        public TestableSpeedBoostEffect(int maxStacks)
        {
            MaxStacks = maxStacks;
        }
    }

    private RecordingTarget target;
    private SpeedBoostEffect effect;

    [SetUp]
    public void SetUp()
    {
        target = new RecordingTarget();
        effect = CreateEffect();
    }

    /// <summary>
    /// Builds the effect with the Speed Boost default of 3 max stacks.
    /// </summary>
    private static SpeedBoostEffect CreateEffect()
    {
        return new TestableSpeedBoostEffect(3);
    }

    // ---------- Multiplier values (defaults table, Section 3.1) ----------

    [TestCase(1, 1.5f)]
    [TestCase(2, 2.0f)]
    [TestCase(3, 2.5f)]
    public void Apply_ValidStackCount_SendsExpectedMultiplier(int stackCount, float expected)
    {
        effect.Apply(target, stackCount);

        Assert.That(target.SpeedMultipliersSent.Count, Is.EqualTo(1));
        Assert.That(target.SpeedMultipliersSent[0], Is.EqualTo(expected).Within(Tolerance));
    }

    [TestCase(4)]
    [TestCase(10)]
    [TestCase(int.MaxValue)]
    public void Apply_StackCountAboveMax_SendsCapOf2_5(int stackCount)
    {
        effect.Apply(target, stackCount);

        Assert.That(target.SpeedMultipliersSent.Count, Is.EqualTo(1));
        Assert.That(target.SpeedMultipliersSent[0], Is.EqualTo(2.5f).Within(Tolerance));
    }

    // ---------- Max stack boundary (Section 3.6) ----------

    [Test]
    public void Apply_FourStacksInARow_SendsBoundarySequence()
    {
        effect.Apply(target, 1);
        effect.Apply(target, 2);
        effect.Apply(target, 3);
        effect.Apply(target, 4);

        float[] expected = { 1.5f, 2.0f, 2.5f, 2.5f };
        Assert.That(target.SpeedMultipliersSent.Count, Is.EqualTo(expected.Length));
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(target.SpeedMultipliersSent[i], Is.EqualTo(expected[i]).Within(Tolerance),
                $"Pickup {i + 1} sent the wrong multiplier.");
        }
    }

    // ---------- Range guarantee (Section 3.2) ----------

    [Test]
    public void Apply_AnyValidStackCount_StaysWithin1_0And2_5()
    {
        for (int stacks = 1; stacks <= 100; stacks++)
        {
            effect.Apply(target, stacks);
        }

        foreach (float value in target.SpeedMultipliersSent)
        {
            Assert.That(value, Is.InRange(1.0f, 2.5f));
        }
    }

    /// <summary>
    /// Bug this caught: Apply() once relied only on Min(stackCount, MaxStacks)
    /// to cap the multiplier, treating the 2.5 clamp as redundant. With
    /// MaxStacks misconfigured to 10, it sent 1.0 + 0.5 x 10 = 6.0 instead of
    /// 2.5. Fixed by clamping to SpeedBoostEffect.MaxMultiplier as a separate
    /// hard cap, independent of MaxStacks.
    /// </summary>
    [Test]
    public void Apply_MisconfiguredMaxStacks_StillCapsAt2_5()
    {
        SpeedBoostEffect badEffect = new TestableSpeedBoostEffect(10);

        badEffect.Apply(target, 10);

        Assert.That(target.SpeedMultipliersSent[0], Is.EqualTo(2.5f).Within(Tolerance));
    }

    // ---------- Adapter state ----------

    [Test]
    public void Apply_UpdatesAdapterLastValueSent()
    {
        effect.Apply(target, 2);

        Assert.That(target.SpeedMultiplier, Is.EqualTo(2.0f).Within(Tolerance));
    }

    [Test]
    public void Apply_SendsExactlyOneCallPerApply()
    {
        effect.Apply(target, 1);
        effect.Apply(target, 1);
        effect.Apply(target, 1);

        Assert.That(target.SpeedMultipliersSent.Count, Is.EqualTo(3));
    }

    // ---------- Dynamic binding (Section 3.5) ----------

    [Test]
    public void Apply_ThroughBaseReference_OnlyCallsSetSpeedMultiplier()
    {
        PowerUpEffect asBase = effect;

        asBase.Apply(target, 1);

        Assert.That(target.SpeedMultipliersSent.Count, Is.EqualTo(1),
            "Override did not run; check that Apply is virtual in PowerUpEffect.");
        Assert.That(target.HealAmountsSent, Is.Empty, "Speed Boost must not call Heal.");
        Assert.That(target.PickupRadiiSent, Is.Empty, "Speed Boost must not change pickup radius.");
    }

    // ---------- Invalid input ----------

    [Test]
    public void Apply_NullTarget_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<System.ArgumentNullException>(() => effect.Apply(null, 1));
        Assert.That(ex.ParamName, Is.EqualTo("target"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void Apply_StackCountBelowOne_ThrowsAndSendsNothing(int stackCount)
    {
        var ex = Assert.Throws<System.ArgumentOutOfRangeException>(
            () => effect.Apply(target, stackCount));

        Assert.That(ex.ParamName, Is.EqualTo("stackCount"));
        Assert.That(target.SpeedMultipliersSent, Is.Empty,
            "Nothing should reach TL6 when the stack count is invalid.");
    }
}