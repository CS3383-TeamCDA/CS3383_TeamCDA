using NUnit.Framework;

namespace EscapeThe90s.PowerUps.Tests
{
    public class BindingTestsTL3
    {
        // Test-only subclass that keeps the base Apply(), so we can compare against an override.
        private class PlainEffect : PowerUpEffect
        {
            public PlainEffect() : base(1f, false, 1)
            {
            }
        }

        private MockPowerUpTarget target;

        [SetUp]
        public void SetUp()
        {
            target = new MockPowerUpTarget();
        }

        #region Dynamic Binding

        [Test]
        public void BaseRef_ToSubclass_UsesOverride()
        {
            PowerUpEffect effect = new SpeedBoostEffect();
            effect.Apply(target, 1);
            Assert.AreEqual(1.5f, target.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void BaseRef_ToSubclass_NoBindingChange()
        {
            PowerUpEffect effect = new PlainEffect();
            effect.Apply(target, 1);
            Assert.AreEqual(1f, target.SpeedMultiplier, 0.0001f);
            Assert.AreEqual(1.5f, target.PickupRadius, 0.0001f);
            Assert.AreEqual(0, target.HealCalls);
        }

        #endregion

        #region Effect Behavior

        [Test]
        public void SpeedBoost_ThreeStacks_IsTwoPointFive()
        {
            new SpeedBoostEffect().Apply(target, 3);
            Assert.AreEqual(2.5f, target.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void Shield_Apply_HealsTwentyFive()
        {
            new ShieldEffect().Apply(target, 1);
            Assert.AreEqual(1, target.HealCalls);
            Assert.AreEqual(ShieldEffect.HealAmount, target.TotalHealed);
        }

        [Test]
        public void Magnet_SnapshotThenRemove_RestoresRadius()
        {
            PowerUpEffect effect = new MagnetEffect();
            StatSnapshot snapshot = effect.CreateSnapshot(target);
            effect.Apply(target, 1);
            Assert.AreEqual(MagnetEffect.BoostedRadius, target.PickupRadius, 0.0001f);

            effect.Remove(target, snapshot);
            Assert.AreEqual(1.5f, target.PickupRadius, 0.0001f);
        }

        #endregion
    }
}
