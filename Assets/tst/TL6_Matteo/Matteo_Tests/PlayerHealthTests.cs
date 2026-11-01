
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
}
