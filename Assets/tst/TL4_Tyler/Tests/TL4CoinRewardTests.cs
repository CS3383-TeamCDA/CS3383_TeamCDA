using NUnit.Framework;
using UnityEngine;

public class TL4CoinRewardTests
{
    private GameObject root;
    private EnemySpawner spawner;
    private EnemyBase enemy;

    [SetUp]
    public void Setup()
    {
        root = new GameObject("Coin reward test");
        spawner = root.AddComponent<EnemySpawner>();
    }

    [TearDown]
    public void Cleanup()
    {
        if (enemy != null) Object.DestroyImmediate(enemy.gameObject);
        if (root != null) Object.DestroyImmediate(root);
    }

    [TestCase(EnemyType.Ground)]
    [TestCase(EnemyType.Flying)]
    [TestCase(EnemyType.Turret)]
    [TestCase(EnemyType.Aim)]
    public void DeathAwardsLevelScaledCoinsExactlyOnce(EnemyType type)
    {
        spawner.SetSpawnLevel(3);
        enemy = spawner.CreateEnemy(type);
        int notifications = 0, coins = 0;
        spawner.OnCoinsAwarded += amount => { notifications++; coins += amount; };
        Assert.IsFalse(enemy.TakeDamage(enemy.MaxHealth - 1));
        Assert.AreEqual(0, notifications);
        enemy.TakeDamage(1);
        enemy.TakeDamage(1000);
        Assert.AreEqual(1, notifications);
        Assert.AreEqual(9, coins, "Level 3 reward is 5 base coins + 2 per additional level.");
    }

    [Test]
    public void DespawningLivingEnemyDoesNotAwardCoins()
    {
        enemy = spawner.CreateEnemy(EnemyType.Ground);
        int coins = 0;
        spawner.OnCoinsAwarded += amount => coins += amount;
        Object.DestroyImmediate(enemy.gameObject);
        Assert.AreEqual(0, coins);
    }

    [Test]
    public void DestroyedSpawnerUnsubscribesFromEnemyDeaths()
    {
        enemy = spawner.CreateEnemy(EnemyType.Ground);
        int coins = 0;
        spawner.OnCoinsAwarded += amount => coins += amount;
        Object.DestroyImmediate(root);
        enemy.TakeDamage(enemy.MaxHealth);
        Assert.AreEqual(0, coins);
    }
}
