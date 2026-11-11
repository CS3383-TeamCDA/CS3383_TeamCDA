using EscapeThe90s.Contracts;
using NUnit.Framework;
using UnityEngine;

public class TL4ContractTests
{
    [Test]
    public void ContractReportsOneDefeatWithIndependentRewardsAndPosition()
    {
        var root = new GameObject("Contract spawner");
        EnemyBase enemy = null;
        try
        {
            IEnemySpawner service = root.AddComponent<EnemySpawner>();
            service.SetSpawnLevel(3);
            enemy = root.GetComponent<EnemySpawner>().CreateEnemy(EnemyType.Ground, new Vector3(12, 7, 0));
            typeof(EnemyBase).GetField("scoreValue", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(enemy, 42);
            int notifications = 0;
            EnemyDefeatedInfo received = default;
            System.Action<EnemyDefeatedInfo> handler = info => { notifications++; received = info; };
            service.OnEnemyDefeated += handler;
            enemy.TakeDamage(enemy.MaxHealth);
            enemy.TakeDamage(enemy.MaxHealth);
            Assert.AreEqual(1, notifications);
            Assert.AreEqual(EnemyType.Ground, received.Type);
            Assert.AreEqual(3, received.Level);
            Assert.AreEqual(9, received.CoinReward);
            Assert.AreEqual(42, received.ScoreValue);
            Assert.AreEqual(12, received.Position.X);
            Assert.AreEqual(7, received.Position.Y);
            Assert.AreEqual(0, service.AliveCount);
            service.OnEnemyDefeated -= handler;
        }
        finally
        {
            if (enemy != null) Object.DestroyImmediate(enemy.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ContractDoesNotReportLivingDespawn()
    {
        var root = new GameObject("Contract spawner");
        try
        {
            var spawner = root.AddComponent<EnemySpawner>();
            IEnemySpawner service = spawner;
            int notifications = 0;
            service.OnEnemyDefeated += _ => notifications++;
            var enemy = spawner.CreateEnemy(EnemyType.Ground);
            Object.DestroyImmediate(enemy.gameObject);
            Assert.AreEqual(0, notifications);
            Assert.AreEqual(0, service.AliveCount);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
