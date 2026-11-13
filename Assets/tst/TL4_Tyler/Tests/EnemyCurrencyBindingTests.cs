using System;
using EscapeThe90s.Contracts;
using NUnit.Framework;
using UnityEngine;

public class EnemyCurrencyBindingTests
{
    private sealed class Wallet : IMetaProgressionManager
    {
        public int Currency { get; private set; }
        public event Action<int> OnCurrencyChanged;
        public int GetUpgradeLevel(UpgradeType type) => 0;
        public float GetEffectValue(UpgradeType type) => 0;
        public void AddCurrency(int amount)
        {
            Currency += amount;
            OnCurrencyChanged?.Invoke(Currency);
        }
    }

    [Test]
    public void DefeatCreditsOnceAndReenableDoesNotDuplicateSubscription()
    {
        var root = new GameObject("Currency integration test");
        EnemyBase enemy = null;
        try
        {
            var spawner = root.AddComponent<EnemySpawner>();
            var binding = root.AddComponent<EnemyCurrencyBinding>();
            var wallet = new Wallet();
            binding.Inject((IEnemySpawner)spawner);
            binding.Inject(wallet);
            binding.TrackFulfilledType(typeof(IEnemySpawner));
            binding.TrackFulfilledType(typeof(IMetaProgressionManager));
            binding.ProcessActivationGate();
            binding.enabled = false;
            binding.enabled = true;
            spawner.SetSpawnLevel(3);
            enemy = spawner.CreateEnemy(EnemyType.Ground);
            enemy.TakeDamage(enemy.MaxHealth);
            enemy.TakeDamage(enemy.MaxHealth);
            Assert.AreEqual(9, wallet.Currency);
            UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            binding.enabled = false;
            enemy = spawner.CreateEnemy(EnemyType.Ground);
            enemy.TakeDamage(enemy.MaxHealth);
            Assert.AreEqual(9, wallet.Currency, "Disabled binding must unsubscribe.");
        }
        finally
        {
            if (enemy != null) UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
