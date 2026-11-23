using System;
using System.Text.RegularExpressions;
using EscapeThe90s.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EscapeThe90s.PowerUps.Tests
{
    /// <summary>
    /// Tests PowerUpManager as the Strategy Client and Memento Caretaker: stacking, timers,
    /// restoring saved stats, and the IPowerUpSpawner contract other teams use.
    /// Runs in Play Mode so AddComponent calls Awake (which registers the strategies).
    /// Tick() is called directly, so no frames need to pass.
    /// </summary>
    public class PowerUpManagerTests
    {
        private static readonly float SpeedDuration = new SpeedBoostEffect().Duration;
        private static readonly float MagnetDuration = new MagnetEffect().Duration;

        private PowerUpManager manager;
        private MockPowerUpTarget target;

        [SetUp]
        public void SetUp()
        {
            manager = new GameObject("PowerUpManager").AddComponent<PowerUpManager>();
            target = new MockPowerUpTarget();
            manager.SetTarget(target);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(manager.gameObject);
        }

        /// <summary>Simulates the player touching a pickup of this type.</summary>
        private void Collect(PowerUpType type, Vector3 position = default)
        {
            PowerUpItem item = new GameObject("Item", typeof(BoxCollider2D)).AddComponent<PowerUpItem>();
            item.transform.position = position;
            item.Initialize(type, manager);
            manager.HandlePickup(item);
        }

        #region Speed Boost stacking and expiry

        [Test]
        public void SpeedBoost_FirstPickup_AppliesOneStack()
        {
            Collect(PowerUpType.SpeedBoost);
            Assert.AreEqual(1.5f, target.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void SpeedBoost_FourPickups_StopsAtThreeStacks()
        {
            for (int i = 0; i < 4; i++) Collect(PowerUpType.SpeedBoost);
            Assert.AreEqual(2.5f, target.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void SpeedBoost_Expires_RestoresOriginalMultiplier()
        {
            Collect(PowerUpType.SpeedBoost);
            Collect(PowerUpType.SpeedBoost);

            manager.Tick(SpeedDuration + 0.1f);

            Assert.AreEqual(1f, target.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void SpeedBoost_BeforeDuration_StillActive()
        {
            Collect(PowerUpType.SpeedBoost);

            manager.Tick(SpeedDuration - 0.1f);

            Assert.AreEqual(1.5f, target.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void SpeedBoost_RepeatPickup_RefreshesTimer()
        {
            Collect(PowerUpType.SpeedBoost);
            manager.Tick(SpeedDuration - 1f);
            Collect(PowerUpType.SpeedBoost);          // timer back to full

            manager.Tick(SpeedDuration - 1f);         // past the first pickup's end
            Assert.AreEqual(2f, target.SpeedMultiplier, 0.0001f, "Second pickup should have refreshed the timer.");

            manager.Tick(1.1f);
            Assert.AreEqual(1f, target.SpeedMultiplier, 0.0001f);
        }

        #endregion

        #region Magnet and Memento

        /// <summary>
        /// Guards the "never re-snapshot" rule: if a second pickup saved the boosted 6.0 as the
        /// original, the radius would never shrink back when the effect ends.
        /// </summary>
        [Test]
        public void Magnet_RepeatPickup_StillRestoresOriginalRadius()
        {
            Collect(PowerUpType.Magnet);
            Collect(PowerUpType.Magnet);

            manager.Tick(MagnetDuration + 0.1f);

            Assert.AreEqual(1.5f, target.PickupRadius, 0.0001f);
        }

        [Test]
        public void MagnetAndSpeedBoost_ExpireIndependently()
        {
            Collect(PowerUpType.SpeedBoost);
            Collect(PowerUpType.Magnet);

            manager.Tick(SpeedDuration + 0.1f);       // 6 s: speed ends, magnet (8 s) does not
            Assert.AreEqual(1f, target.SpeedMultiplier, 0.0001f);
            Assert.AreEqual(MagnetEffect.BoostedRadius, target.PickupRadius, 0.0001f);

            manager.Tick(MagnetDuration - SpeedDuration);
            Assert.AreEqual(1.5f, target.PickupRadius, 0.0001f);
        }

        #endregion

        #region Shield

        [Test]
        public void Shield_Pickups_HealWithoutTimer()
        {
            Collect(PowerUpType.Shield);
            Collect(PowerUpType.Shield);

            manager.Tick(100f);

            Assert.AreEqual(2 * ShieldEffect.HealAmount, target.TotalHealed);
            Assert.AreEqual(1f, target.SpeedMultiplier, 0.0001f);
            Assert.AreEqual(1.5f, target.PickupRadius, 0.0001f);
        }

        #endregion

        #region IPowerUpSpawner contract

        [Test]
        public void Contract_SpeedBoostPickup_ReportsStackAndTime()
        {
            IPowerUpSpawner spawner = manager;
            PowerUpCollectedInfo last = default;
            spawner.OnPowerUpCollected += info => last = info;

            Collect(PowerUpType.SpeedBoost, new Vector3(3f, 4f, 0f));
            Collect(PowerUpType.SpeedBoost, new Vector3(3f, 4f, 0f));

            Assert.AreEqual(PowerUpType.SpeedBoost, last.Type);
            Assert.AreEqual(2, last.StackCount);
            Assert.AreEqual(SpeedDuration, last.RemainingSeconds, 0.0001f);
            Assert.IsFalse(last.IsPermanent);
            Assert.AreEqual(3f, last.Position.X, 0.0001f);
            Assert.AreEqual(4f, last.Position.Y, 0.0001f);
        }

        [Test]
        public void Contract_ShieldPickup_IsPermanentWithNoTime()
        {
            IPowerUpSpawner spawner = manager;
            PowerUpCollectedInfo last = default;
            spawner.OnPowerUpCollected += info => last = info;

            Collect(PowerUpType.Shield);

            Assert.AreEqual(PowerUpType.Shield, last.Type);
            Assert.IsTrue(last.IsPermanent);
            Assert.AreEqual(0f, last.RemainingSeconds, 0.0001f);
        }

        [Test]
        public void Contract_ThrowingSubscriber_DoesNotStopOthers()
        {
            IPowerUpSpawner spawner = manager;
            int calls = 0;
            spawner.OnPowerUpCollected += info => throw new InvalidOperationException("bad subscriber");
            spawner.OnPowerUpCollected += info => calls++;
            LogAssert.Expect(LogType.Exception, new Regex("bad subscriber"));

            Collect(PowerUpType.Shield);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(ShieldEffect.HealAmount, target.TotalHealed, "The effect must still apply.");
        }

        [Test]
        public void Contract_Unsubscribe_StopsEvents()
        {
            IPowerUpSpawner spawner = manager;
            int calls = 0;
            Action<PowerUpCollectedInfo> handler = info => calls++;
            spawner.OnPowerUpCollected += handler;
            spawner.OnPowerUpCollected -= handler;

            Collect(PowerUpType.Shield);

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Contract_SpawnNonFinitePosition_Throws()
        {
            IPowerUpSpawner spawner = manager;
            Assert.Throws<ArgumentException>(
                () => spawner.SpawnPowerUp(new WorldPosition2D(float.NaN, 0f), PowerUpType.Shield));
        }

        [Test]
        public void Contract_SpawnUndefinedType_Throws()
        {
            IPowerUpSpawner spawner = manager;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => spawner.SpawnPowerUp(new WorldPosition2D(0f, 0f), (PowerUpType)99));
        }

        [Test]
        public void Contract_SpawnWithoutDefinition_LogsErrorAndSpawnsNothing()
        {
            IPowerUpSpawner spawner = manager;
            LogAssert.Expect(LogType.Error, new Regex("No PowerUpDefinition assigned for Magnet"));
            int before = Object.FindObjectsByType<PowerUpItem>(FindObjectsInactive.Exclude).Length;

            spawner.SpawnPowerUp(new WorldPosition2D(0f, 0f), PowerUpType.Magnet);

            Assert.AreEqual(before, Object.FindObjectsByType<PowerUpItem>(FindObjectsInactive.Exclude).Length);
        }

        #endregion

        #region Missing player

        [Test]
        public void Pickup_NoTarget_IsIgnoredWithError()
        {
            PowerUpManager noPlayer = new GameObject("NoPlayerManager").AddComponent<PowerUpManager>();
            bool raised = false;
            ((IPowerUpSpawner)noPlayer).OnPowerUpCollected += info => raised = true;
            LogAssert.Expect(LogType.Error, new Regex("Pickup ignored: no IPowerUpTarget"));

            PowerUpItem item = new GameObject("Item", typeof(BoxCollider2D)).AddComponent<PowerUpItem>();
            item.Initialize(PowerUpType.Shield, noPlayer);
            noPlayer.HandlePickup(item);

            Assert.IsFalse(raised);
            Object.Destroy(noPlayer.gameObject);
        }

        #endregion
    }
}
