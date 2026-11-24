using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.MetaProgression;
using EscapeThe90s.Contracts;

    public class UpgradeSystemTests
    {
        private const int Max = 3;
        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "metaprogression.json");

        private GameObject go;
        private MetaProgressionManager manager;
        private string backup;

        // ---- Setup / teardown: isolate from the real save file and static cache ----

        [SetUp]
        public void SetUp()
        {
            backup = File.Exists(SavePath) ? File.ReadAllText(SavePath) : null;
            if (File.Exists(SavePath)) File.Delete(SavePath);
            ResetCache();

            go = new GameObject("MetaProgressionManager");
            manager = go.AddComponent<MetaProgressionManager>();

            var defs = new List<UpgradeDefinition>();
            foreach (UpgradeType t in Enum.GetValues(typeof(UpgradeType)))
                defs.Add(Def(t, Max));
            typeof(MetaProgressionManager)
                .GetField("definitions", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(manager, defs);

            manager.RestoreSnapshot(Snapshot(0, 0));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(go);
            if (backup != null) File.WriteAllText(SavePath, backup);
            else if (File.Exists(SavePath)) File.Delete(SavePath);
            ResetCache();
        }

        // ---- Helpers ----

        private static void ResetCache() =>
            typeof(MetaProgressionManager)
                .GetField("cache", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);

        private static int TypeCount => Enum.GetValues(typeof(UpgradeType)).Length;

        // UpgradeDefinition has only private serialized fields, so build it from JSON.
        private static UpgradeDefinition Def(UpgradeType t, int max) =>
            JsonUtility.FromJson<UpgradeDefinition>(
                $"{{\"type\":{(int)t},\"baseValue\":1,\"valuePerLevel\":5,\"baseCost\":10,\"costGrowth\":2,\"maxLevel\":{max}}}");

        private static SaveData Snapshot(int currency, int level)
        {
            var levels = new int[TypeCount];
            for (int i = 0; i < levels.Length; i++) levels[i] = level;
            return new SaveData { currency = currency, upgradeLevels = levels };
        }

        private int CostOfNext(UpgradeType t) =>
            Def(t, Max).GetCost(manager.GetUpgradeLevel(t));

        private class FakePlayer : IPlayerPermanentUpgradeReceiver
        {
            public float health, attack, speed;
            public void SetPermanentHealthBonus(float v) => health = v;
            public void SetPermanentAttackBonus(float v) => attack = v;
            public void SetPermanentSpeedBonus(float v) => speed = v;
        }

        // ---- UpgradeDefinition ----

        [Test]
        public void Definition_EffectValue_IsBasePlusPerLevel()
        {
            var def = Def(UpgradeType.Health, Max);
            Assert.AreEqual(1f, def.GetEffectValue(0));
            Assert.AreEqual(11f, def.GetEffectValue(2));
        }

        [Test]
        public void Definition_Cost_IncreasesWithLevel()
        {
            var def = Def(UpgradeType.Health, Max);
            Assert.Greater(def.GetCost(1), def.GetCost(0));
            Assert.Greater(def.GetCost(2), def.GetCost(1));
        }

        [Test]
        public void Definition_Effect_MatchesType()
        {
            Assert.IsInstanceOf<HealthEffect>(Def(UpgradeType.Health, Max).Effect);
            Assert.IsInstanceOf<SpeedEffect>(Def(UpgradeType.Speed, Max).Effect);
            Assert.IsInstanceOf<MagnetRangeEffect>(Def(UpgradeType.MagnetRange, Max).Effect);
            Assert.IsInstanceOf<AttackEffect>(Def(UpgradeType.Attack, Max).Effect);
        }

        // ---- Currency ----

        [Test]
        public void AddCurrency_Positive_IncreasesAndRaisesEvent()
        {
            int raised = -1;
            manager.OnCurrencyChanged += c => raised = c;
            manager.AddCurrency(25);
            Assert.AreEqual(25, manager.Currency);
            Assert.AreEqual(25, raised);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void AddCurrency_ZeroOrNegative_IsIgnored(int amount)
        {
            bool raised = false;
            manager.OnCurrencyChanged += _ => raised = true;
            manager.AddCurrency(amount);
            Assert.AreEqual(0, manager.Currency);
            Assert.IsFalse(raised);
        }

        // ---- CanPurchase ----

        [Test]
        public void CanPurchase_NotEnoughCurrency_ReturnsFalse()
        {
            manager.RestoreSnapshot(Snapshot(CostOfNext(UpgradeType.Health) - 1, 0));
            Assert.IsFalse(manager.CanPurchase(UpgradeType.Health));
        }

        [Test]
        public void CanPurchase_EnoughCurrency_ReturnsTrue()
        {
            manager.RestoreSnapshot(Snapshot(CostOfNext(UpgradeType.Health), 0));
            Assert.IsTrue(manager.CanPurchase(UpgradeType.Health));
        }

        [Test]
        public void CanPurchase_AtMaxLevel_ReturnsFalse()
        {
            manager.RestoreSnapshot(Snapshot(100000, Max));
            Assert.IsFalse(manager.CanPurchase(UpgradeType.Health));
        }

        // ---- PurchaseUpgrade ----

        [Test]
        public void Purchase_Success_DeductsCostRaisesLevelAndRaisesEvent()
        {
            int cost = CostOfNext(UpgradeType.Speed);
            manager.RestoreSnapshot(Snapshot(cost + 7, 0));
            int raised = -1;
            manager.OnCurrencyChanged += c => raised = c;

            Assert.IsTrue(manager.PurchaseUpgrade(UpgradeType.Speed));

            Assert.AreEqual(7, manager.Currency);
            Assert.AreEqual(1, manager.GetUpgradeLevel(UpgradeType.Speed));
            Assert.AreEqual(7, raised);
        }

        [Test]
        public void Purchase_Fails_ChangesNothing()
        {
            manager.RestoreSnapshot(Snapshot(1, 0));
            bool raised = false;
            manager.OnCurrencyChanged += _ => raised = true;

            Assert.IsFalse(manager.PurchaseUpgrade(UpgradeType.Speed));

            Assert.AreEqual(1, manager.Currency);
            Assert.AreEqual(0, manager.GetUpgradeLevel(UpgradeType.Speed));
            Assert.IsFalse(raised);
        }

        [Test]
        public void Purchase_StopsAtMaxLevel()
        {
            manager.RestoreSnapshot(Snapshot(100000, 0));
            for (int i = 0; i < Max + 2; i++) manager.PurchaseUpgrade(UpgradeType.Attack);
            Assert.AreEqual(Max, manager.GetUpgradeLevel(UpgradeType.Attack));
        }

        [Test]
        public void Purchase_IsPersistedToDisk()
        {
            manager.RestoreSnapshot(Snapshot(CostOfNext(UpgradeType.Health), 0));
            manager.PurchaseUpgrade(UpgradeType.Health);

            SaveData loaded = SaveSystem.Load();
            Assert.AreEqual(0, loaded.currency);
            Assert.AreEqual(1, loaded.upgradeLevels[(int)UpgradeType.Health]);
        }

        [Test]
        public void Purchase_IncreasesEffectValue()
        {
            float before = manager.GetEffectValue(UpgradeType.Health);
            manager.RestoreSnapshot(Snapshot(CostOfNext(UpgradeType.Health), 0));
            manager.PurchaseUpgrade(UpgradeType.Health);
            Assert.Greater(manager.GetEffectValue(UpgradeType.Health), before);
        }

        // ---- Snapshot / Restore ----

        [Test]
        public void Snapshot_RoundTrip_RestoresCurrencyAndLevels()
        {
            manager.RestoreSnapshot(Snapshot(40, 2));
            SaveData snap = manager.CreateSnapshot();

            manager.RestoreSnapshot(Snapshot(0, 0));
            manager.RestoreSnapshot(snap);

            Assert.AreEqual(40, manager.Currency);
            Assert.AreEqual(2, manager.GetUpgradeLevel(UpgradeType.Health));
        }

        [Test]
        public void Snapshot_IsACopy()
        {
            SaveData snap = manager.CreateSnapshot();
            snap.upgradeLevels[(int)UpgradeType.Health] = 3;
            Assert.AreEqual(0, manager.GetUpgradeLevel(UpgradeType.Health));
        }

        [Test]
        public void Restore_ClampsLevelsToMax()
        {
            manager.RestoreSnapshot(Snapshot(0, Max + 10));
            Assert.AreEqual(Max, manager.GetUpgradeLevel(UpgradeType.Health));
        }

        [Test]
        public void Restore_NegativeCurrency_ClampedToZero()
        {
            manager.RestoreSnapshot(Snapshot(-50, 0));
            Assert.AreEqual(0, manager.Currency);
        }

        [Test]
        public void Restore_Null_IsIgnored()
        {
            manager.RestoreSnapshot(Snapshot(30, 1));
            manager.RestoreSnapshot(null);
            Assert.AreEqual(30, manager.Currency);
            Assert.AreEqual(1, manager.GetUpgradeLevel(UpgradeType.Health));
        }

        [Test]
        public void Restore_ShortLevelArray_MissingEntriesDefaultToZero()
        {
            manager.RestoreSnapshot(new SaveData { currency = 0, upgradeLevels = new[] { 2 } });
            Assert.AreEqual(2, manager.GetUpgradeLevel((UpgradeType)0));
            Assert.AreEqual(0, manager.GetUpgradeLevel((UpgradeType)(TypeCount - 1)));
        }

        // ---- Starting bonuses ----

        [Test]
        public void ApplyStartingBonuses_SendsUpgradeValuesToPlayer()
        {
            manager.RestoreSnapshot(Snapshot(0, 2));
            var player = new FakePlayer();
            manager.RegisterPlayer(player);   // no game state injected, so nothing applied yet
            manager.ApplyStartingBonuses();

            Assert.AreEqual(manager.GetEffectValue(UpgradeType.Health), player.health);
            Assert.AreEqual(manager.GetEffectValue(UpgradeType.Attack), player.attack);
            Assert.AreEqual(manager.GetEffectValue(UpgradeType.Speed), player.speed);
        }
    }
