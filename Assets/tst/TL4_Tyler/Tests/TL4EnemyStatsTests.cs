using NUnit.Framework;
using UnityEngine;

public class TL4EnemyStatsTests
{
    private GameObject instance;
    private GroundEnemy enemy;

    [SetUp] public void SetUp()
    {
        instance = new GameObject("Stats test");
        enemy = instance.AddComponent<GroundEnemy>();
    }
    [TearDown] public void TearDown() { Object.DestroyImmediate(instance); }

    [Test] public void LevelScalesAllStatsFromBaseWithoutCompounding()
    {
        enemy.SetLevel(3);
        Assert.AreEqual(140, enemy.MaxHealth);
        Assert.AreEqual(140, enemy.CurrentHealth);
        Assert.AreEqual(9, enemy.CoinReward);
        Assert.AreEqual(14, enemy.MeleeDamage);
        Assert.AreEqual(12, enemy.RangedDamage);
        enemy.SetLevel(3);
        Assert.AreEqual(140, enemy.CurrentHealth);
        enemy.SetLevel(1);
        Assert.AreEqual(100, enemy.MaxHealth);
        Assert.AreEqual(5, enemy.CoinReward);
    }

    [Test] public void ChangingLevelPreservesHealthPercentageAndDoesNotResurrect()
    {
        enemy.TakeDamage(50);
        enemy.SetLevel(3);
        Assert.AreEqual(70, enemy.CurrentHealth);
        int rewards = 0;
        enemy.Defeated += e => rewards += e.CoinReward;
        enemy.TakeDamage(70);
        enemy.SetLevel(4);
        enemy.TakeDamage(1);
        Assert.AreEqual(0, enemy.CurrentHealth);
        Assert.IsTrue(enemy.IsDefeated);
        Assert.AreEqual(9, rewards);
    }

    [TestCase(EnemyAttackMode.MeleeOnly, true, false)]
    [TestCase(EnemyAttackMode.RangedOnly, false, true)]
    [TestCase(EnemyAttackMode.Both, true, true)]
    public void AttackModeControlsAvailableDamage(EnemyAttackMode mode, bool melee, bool ranged)
    {
        enemy.SetAttackMode(mode);
        Assert.AreEqual(melee, enemy.CanMelee);
        Assert.AreEqual(ranged, enemy.CanRanged);
        Assert.AreEqual(melee ? 10 : 0, enemy.MeleeDamage);
        Assert.AreEqual(ranged ? 8 : 0, enemy.RangedDamage);
    }

    [Test] public void ScenePlacedEnemiesHaveCorrectIdentityWithoutSpawner()
    {
        Assert.AreEqual(EnemyType.Ground, enemy.Type);
        Assert.AreEqual(EnemyMovementType.Ground, enemy.MovementType);
        var flying = instance.AddComponent<FlyingEnemy>();
        var turret = instance.AddComponent<TurretHazard>();
        var aim = instance.AddComponent<AimEnemy>();
        Assert.AreEqual(EnemyType.Flying, flying.Type);
        Assert.AreEqual(EnemyMovementType.Flying, flying.MovementType);
        Assert.AreEqual(EnemyType.Turret, turret.Type);
        Assert.AreEqual(EnemyMovementType.Static, turret.MovementType);
        Assert.AreEqual(EnemyType.Aim, aim.Type);
        Assert.AreEqual(EnemyMovementType.Static, aim.MovementType);
    }

    [Test] public void SpawnerAppliesLevelBeforeSpawnNotification()
    {
        var spawner = instance.AddComponent<EnemySpawner>();
        spawner.SetSpawnLevel(5);
        int observedHealth = 0;
        spawner.OnEnemySpawned += e => observedHealth = e.CurrentHealth;
        var spawned = spawner.CreateEnemy(EnemyType.Ground);
        try { Assert.AreEqual(180, observedHealth); Assert.AreEqual(5, spawned.Level); }
        finally { Object.DestroyImmediate(spawned.gameObject); }
    }

    [Test] public void InvalidLevelsAreRejectedAndExtremeScalingDoesNotOverflow()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => enemy.SetLevel(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => enemy.SetAttackMode((EnemyAttackMode)999));
        enemy.SetLevel(int.MaxValue);
        Assert.AreEqual(int.MaxValue, enemy.MaxHealth);
        Assert.AreEqual(int.MaxValue, enemy.CurrentHealth);
        Assert.IsFalse(enemy.TakeDamage(1));
        Assert.AreEqual(int.MaxValue - 1, enemy.CurrentHealth);
    }
}
