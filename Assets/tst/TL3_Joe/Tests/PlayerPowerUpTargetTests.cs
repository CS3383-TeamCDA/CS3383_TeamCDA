using EscapeThe90s.Contracts;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EscapeThe90s.PowerUps.Tests
{
    /// <summary>
    /// Tests the adapter between power-ups and TL6's player: Heal reaches PlayerHealth and
    /// the speed multiplier reaches the controller. PlayerPowerUpTarget looks up its
    /// neighbours in Awake, so they are added to the GameObject before it.
    /// </summary>
    public class PlayerPowerUpTargetTests
    {
        private GameObject player;

        [TearDown]
        public void TearDown()
        {
            if (player != null) Object.Destroy(player);
        }

        #region Heal

        [Test]
        public void Heal_ForwardsToPlayerHealth()
        {
            player = new GameObject("Player");
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            PlayerPowerUpTarget adapter = player.AddComponent<PlayerPowerUpTarget>();
            health.TakeDamage(50);

            adapter.Heal(ShieldEffect.HealAmount);

            Assert.AreEqual(health.MaxHealth - 50 + ShieldEffect.HealAmount, health.CurrentHealth);
        }

        [Test]
        public void Heal_AtFullHealth_StaysAtMax()
        {
            player = new GameObject("Player");
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            PlayerPowerUpTarget adapter = player.AddComponent<PlayerPowerUpTarget>();

            adapter.Heal(ShieldEffect.HealAmount);

            Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
        }

        [Test]
        public void Heal_NoPlayerHealth_DoesNotThrow()
        {
            player = new GameObject("Player");
            PlayerPowerUpTarget adapter = player.AddComponent<PlayerPowerUpTarget>();

            Assert.DoesNotThrow(() => adapter.Heal(ShieldEffect.HealAmount));
        }

        #endregion

        #region Speed

        [Test]
        public void SetSpeedMultiplier_ForwardsToController()
        {
            player = new GameObject("Player");
            FakePlayerController controller = player.AddComponent<FakePlayerController>();
            PlayerPowerUpTarget adapter = player.AddComponent<PlayerPowerUpTarget>();

            adapter.SetSpeedMultiplier(2f);

            Assert.AreEqual(2f, controller.LastMultiplier, 0.0001f);
            Assert.AreEqual(2f, adapter.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void SetSpeedMultiplier_NoController_StillTracksValue()
        {
            player = new GameObject("Player");
            PlayerPowerUpTarget adapter = player.AddComponent<PlayerPowerUpTarget>();

            Assert.DoesNotThrow(() => adapter.SetSpeedMultiplier(1.5f));
            Assert.AreEqual(1.5f, adapter.SpeedMultiplier, 0.0001f);
        }

        /// <summary>Full path: pickup, stack, expire, and the controller ends back at 1x.</summary>
        [Test]
        public void SpeedBoost_ThroughManager_ReachesControllerAndResets()
        {
            player = new GameObject("Player");
            FakePlayerController controller = player.AddComponent<FakePlayerController>();
            PlayerPowerUpTarget adapter = player.AddComponent<PlayerPowerUpTarget>();
            PowerUpManager manager = player.AddComponent<PowerUpManager>();
            manager.SetTarget(adapter);

            for (int i = 0; i < 2; i++)
            {
                PowerUpItem item = new GameObject("Item", typeof(BoxCollider2D)).AddComponent<PowerUpItem>();
                item.Initialize(PowerUpType.SpeedBoost, manager);
                manager.HandlePickup(item);
            }
            Assert.AreEqual(2f, controller.LastMultiplier, 0.0001f);

            manager.Tick(new SpeedBoostEffect().Duration + 0.1f);

            Assert.AreEqual(1f, controller.LastMultiplier, 0.0001f);
        }

        #endregion

        #region Pickup radius

        [Test]
        public void PickupRadius_StartsAtBase()
        {
            player = new GameObject("Player");
            PlayerPowerUpTarget adapter = player.AddComponent<PlayerPowerUpTarget>();

            Assert.AreEqual(1.5f, adapter.PickupRadius, 0.0001f);
        }

        #endregion
    }
}
