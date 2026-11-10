
using NUnit.Framework;
using UnityEngine;

public class PlayerHealthTests
{
    private GameObject _player;
    private PlayerHealth _health;

    [SetUp]
    public void SetUp()
    {
        _player = new GameObject("TestPlayer");
        _health = _player.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_player);
    }

    [Test]
    public void PlayerStartsAtMaxHealth()
    {
        Assert.AreEqual(100, _health.MaxHealth);
        Assert.AreEqual(100, _health.CurrentHealth);
    }

    [Test]
    public void TakingDamageReducesHealth()
    {
        _health.TakeDamage(25);

        Assert.AreEqual(75, _health.CurrentHealth);
    }

    [Test]
    public void DamageCannotReduceHealthBelowZero()
    {
        _health.TakeDamage(150);

        Assert.AreEqual(0, _health.CurrentHealth);
    }

    [Test]
    public void HealingRestoresHealth()
    {
        _health.TakeDamage(50);
        _health.Heal(20);

        Assert.AreEqual(70, _health.CurrentHealth);
    }

    [Test]
    public void HealingCannotExceedMaxHealth()
    {
        _health.TakeDamage(20);
        _health.Heal(50);

        Assert.AreEqual(100, _health.CurrentHealth);
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void InvalidDamageIsIgnored(int damage)
    {
        _health.TakeDamage(damage);

        Assert.AreEqual(100, _health.CurrentHealth);
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void InvalidHealingIsIgnored(int amount)
    {
        _health.TakeDamage(30);
        _health.Heal(amount);

        Assert.AreEqual(70, _health.CurrentHealth);
    }

    [Test]
    public void DamageAfterDeathIsIgnored()
    {
        _health.TakeDamage(100);
        _health.TakeDamage(25);

        Assert.AreEqual(0, _health.CurrentHealth);
    }

    [Test]
    public void HealingAfterDeathIsIgnored()
    {
        _health.TakeDamage(100);
        _health.Heal(50);

        Assert.AreEqual(0, _health.CurrentHealth);
    }

    [Test]
    public void HealthChangedEventProvidesCorrectValues()
    {
        int current = -1;
        int max = -1;
        int eventCount = 0;

        _health.OnHealthChanged += (currentHealth, maxHealth) =>
        {
            current = currentHealth;
            max = maxHealth;
            eventCount++;
        };

        _health.TakeDamage(25);

        Assert.AreEqual(75, current);
        Assert.AreEqual(100, max);
        Assert.AreEqual(1, eventCount);
    }

    [Test]
    public void HealingPublishesHealthChanged()
    {
        _health.TakeDamage(50);

        int reportedHealth = -1;
        int eventCount = 0;

        _health.OnHealthChanged += (current, max) =>
        {
            reportedHealth = current;
            eventCount++;
        };

        _health.Heal(20);

        Assert.AreEqual(70, reportedHealth);
        Assert.AreEqual(1, eventCount);
    }

    [Test]
    public void HealingAtFullHealthDoesNotPublishEvent()
    {
        int eventCount = 0;

        _health.OnHealthChanged += (current, max) =>
            eventCount++;

        _health.Heal(25);

        Assert.AreEqual(100, _health.CurrentHealth);
        Assert.AreEqual(0, eventCount);
    }

    [Test]
    public void PlayerDeathEventFiresExactlyOnce()
    {
        int deathCount = 0;

        _health.OnPlayerDied += () => deathCount++;

        _health.TakeDamage(100);
        _health.TakeDamage(50);

        Assert.AreEqual(0, _health.CurrentHealth);
        Assert.AreEqual(1, deathCount);
    }

    [Test]
    public void NonFatalDamageDoesNotPublishDeath()
    {
        int deathCount = 0;

        _health.OnPlayerDied += () => deathCount++;

        _health.TakeDamage(25);

        Assert.AreEqual(0, deathCount);
    }


    [Test]
    public void IncreasingMaxHealthAlsoIncreasesCurrentHealth()
    {
        _health.TakeDamage(40); // 60/100
        _health.SetMaxHealth(125);

        Assert.AreEqual(125, _health.MaxHealth);
        Assert.AreEqual(85, _health.CurrentHealth);
    }

    [Test]
    public void IncreasingMaxHealthAtFullHealthKeepsPlayerFull()
    {
        _health.SetMaxHealth(125);

        Assert.AreEqual(125, _health.MaxHealth);
        Assert.AreEqual(125, _health.CurrentHealth);
    }

    [Test]
    public void MaxHealthCannotBeDecreased()
    {
        _health.SetMaxHealth(75);

        Assert.AreEqual(100, _health.MaxHealth);
        Assert.AreEqual(100, _health.CurrentHealth);
    }

    [Test]
    public void InvalidMaxHealthIsIgnored()
    {
        _health.SetMaxHealth(0);
        _health.SetMaxHealth(-50);

        Assert.AreEqual(100, _health.MaxHealth);
        Assert.AreEqual(100, _health.CurrentHealth);
    }

    [Test]
    public void MaxHealthChangePublishesHealthChangedEvent()
    {
        int eventCount = 0;
        int reportedCurrent = -1;
        int reportedMax = -1;

        _health.OnHealthChanged += (current, max) =>
        {
            eventCount++;
            reportedCurrent = current;
            reportedMax = max;
        };

        _health.SetMaxHealth(125);

        Assert.AreEqual(1, eventCount);
        Assert.AreEqual(125, reportedCurrent);
        Assert.AreEqual(125, reportedMax);
    }


    [Test]
    public void IsDeadReturnsTrueWhenHealthReachesZero()
    {
        Assert.IsFalse(_health.IsDead);

        _health.TakeDamage(100);

        Assert.IsTrue(_health.IsDead);
    }

    [Test]
    public void ResetHealthRestoresFullHealth()
    {
        _health.TakeDamage(75);
        _health.ResetHealth();

        Assert.AreEqual(100, _health.CurrentHealth);
        Assert.IsFalse(_health.IsDead);
    }

    [Test]
    public void ResetHealthPreservesMaxHealthUpgrades()
    {
        _health.SetMaxHealth(150);
        _health.TakeDamage(80);
        _health.ResetHealth();

        Assert.AreEqual(150, _health.MaxHealth);
        Assert.AreEqual(150, _health.CurrentHealth);
    }

    [Test]
    public void ResetHealthPublishesHealthChangedEvent()
    {
        int eventCount = 0;
        int reportedCurrent = -1;
        int reportedMax = -1;

        _health.TakeDamage(50);

        _health.OnHealthChanged += (current, max) =>
        {
            eventCount++;
            reportedCurrent = current;
            reportedMax = max;
        };

        _health.ResetHealth();

        Assert.AreEqual(1, eventCount);
        Assert.AreEqual(100, reportedCurrent);
        Assert.AreEqual(100, reportedMax);
    }

    [Test]
    public void IncreaseMaxHealthAddsSpecifiedAmount()
    {
        _health.TakeDamage(40);
        _health.IncreaseMaxHealth(25);

        Assert.AreEqual(125, _health.MaxHealth);
        Assert.AreEqual(85, _health.CurrentHealth);
    }


    [TestCase(0)]
    [TestCase(-25)]
    public void InvalidMaxHealthIncreaseIsIgnored(int amount)
    {
        int eventCount = 0;

        _health.OnHealthChanged += (current, max) => eventCount++;

        _health.IncreaseMaxHealth(amount);

        Assert.AreEqual(100, _health.MaxHealth);
        Assert.AreEqual(100, _health.CurrentHealth);
        Assert.AreEqual(0, eventCount);
    }

    [Test]
    public void ResetHealthAfterDeathRestoresPlayer()
    {
        _health.TakeDamage(100);

        Assert.IsTrue(_health.IsDead);

        _health.ResetHealth();

        Assert.AreEqual(100, _health.CurrentHealth);
        Assert.IsFalse(_health.IsDead);
    }


}
