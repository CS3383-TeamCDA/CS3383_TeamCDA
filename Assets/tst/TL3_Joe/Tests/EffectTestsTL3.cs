using NUnit.Framework;

namespace EscapeThe90s.PowerUps.Tests
{
    /// <summary>
    /// Tests for MagnetEffect and ShieldEffect on their own, without PowerUpManager.
    /// SpeedBoostEffect has its own file (SpeedBoostEffectTests).
    /// </summary>
    public class EffectTestsTL3
    {
        private MockPowerUpTarget target;

        [SetUp]
        public void SetUp()
        {
            target = new MockPowerUpTarget();
        }

        #region Magnet

        [Test]
        public void Magnet_Settings_EightSecondsNoStacking()
        {
            MagnetEffect effect = new MagnetEffect();
            Assert.AreEqual(8f, effect.Duration, 0.0001f);
            Assert.IsFalse(effect.IsPermanent);
            Assert.AreEqual(1, effect.MaxStacks);
        }

        [Test]
        public void Magnet_Apply_SetsBoostedRadius()
        {
            new MagnetEffect().Apply(target, 1);
            Assert.AreEqual(MagnetEffect.BoostedRadius, target.PickupRadius, 0.0001f);
        }

        [TestCase(2)]
        [TestCase(10)]
        public void Magnet_Apply_IgnoresStackCount(int stackCount)
        {
            new MagnetEffect().Apply(target, stackCount);
            Assert.AreEqual(MagnetEffect.BoostedRadius, target.PickupRadius, 0.0001f);
        }

        [Test]
        public void Magnet_Snapshot_SavesCurrentRadius()
        {
            target.SetPickupRadius(3f);   // e.g. a future permanent magnet-range upgrade

            StatSnapshot snapshot = new MagnetEffect().CreateSnapshot(target);

            Assert.AreEqual(StatKind.PickupRadius, snapshot.Stat);
            Assert.AreEqual(3f, snapshot.Value, 0.0001f);
        }

        [Test]
        public void Magnet_Remove_RestoresNonDefaultRadius()
        {
            target.SetPickupRadius(3f);
            MagnetEffect effect = new MagnetEffect();
            StatSnapshot snapshot = effect.CreateSnapshot(target);

            effect.Apply(target, 1);
            effect.Remove(target, snapshot);

            Assert.AreEqual(3f, target.PickupRadius, 0.0001f);
        }

        [Test]
        public void Magnet_Apply_DoesNotTouchSpeedOrHealth()
        {
            new MagnetEffect().Apply(target, 1);
            Assert.AreEqual(1f, target.SpeedMultiplier, 0.0001f);
            Assert.AreEqual(0, target.HealCalls);
        }

        #endregion

        #region Shield

        [Test]
        public void Shield_Settings_PermanentSingleStack()
        {
            ShieldEffect effect = new ShieldEffect();
            Assert.IsTrue(effect.IsPermanent);
            Assert.AreEqual(1, effect.MaxStacks);
        }

        [Test]
        public void Shield_ApplyTwice_HealsEachTime()
        {
            ShieldEffect effect = new ShieldEffect();
            effect.Apply(target, 1);
            effect.Apply(target, 1);

            Assert.AreEqual(2, target.HealCalls);
            Assert.AreEqual(2 * ShieldEffect.HealAmount, target.TotalHealed);
        }

        [Test]
        public void Shield_CreateSnapshot_ReturnsNull()
        {
            Assert.IsNull(new ShieldEffect().CreateSnapshot(target));
        }

        [Test]
        public void Shield_Apply_DoesNotTouchSpeedOrRadius()
        {
            new ShieldEffect().Apply(target, 1);
            Assert.AreEqual(1f, target.SpeedMultiplier, 0.0001f);
            Assert.AreEqual(1.5f, target.PickupRadius, 0.0001f);
        }

        #endregion
    }
}
