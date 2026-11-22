
using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.Contracts;

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

    // DEFAULT HEALTH

    [Test]
    public void PlayerStartsAtMaxHealth()
    {
        Assert.AreEqual(100, _health.MaxHealth);
        Assert.AreEqual(100, _health.CurrentHealth);
        Assert.IsFalse(_health.IsDead);
    }

    // DAMAGE

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

    [TestCase(0)]
    [TestCase(-10)]
    public void InvalidDamageIsIgnored(int damage)
    {
        _health.TakeDamage(damage);

        Assert.AreEqual(100, _health.CurrentHealth);
    }

    [Test]
    public void DamageAfterDeathIsIgnored()
    {
        _health.TakeDamage(100);
        _health.TakeDamage(25);

        Assert.AreEqual(0, _health.CurrentHealth);
    }

    // HEALING

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
    public void InvalidHealingIsIgnored(int amount)
    {
        _health.TakeDamage(30);
        _health.Heal(amount);

        Assert.AreEqual(70, _health.CurrentHealth);
    }

    [Test]
    public void HealingAfterDeathIsIgnored()
    {
        _health.TakeDamage(100);
        _health.Heal(50);

        Assert.AreEqual(0, _health.CurrentHealth);
    }

    // HEALTH EVENTS

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

    // DEATH

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
    public void IsDeadReturnsTrueWhenHealthReachesZero()
    {
        Assert.IsFalse(_health.IsDead);

        _health.TakeDamage(100);

        Assert.IsTrue(_health.IsDead);
    }

    // PERMANENT HEALTH UPGRADES

    [Test]
    public void PermanentHealthBonusIncreasesMaxAndCurrentHealth()
    {
        _health.SetPermanentHealthBonus(25);

        Assert.AreEqual(125, _health.MaxHealth);
        Assert.AreEqual(125, _health.CurrentHealth);
    }

    [Test]
    public void PermanentHealthBonusPreservesMissingHealth()
    {
        _health.TakeDamage(40); // 60/100

        _health.SetPermanentHealthBonus(25);

        Assert.AreEqual(125, _health.MaxHealth);
        Assert.AreEqual(85, _health.CurrentHealth);
    }

    [Test]
    public void ReapplyingSamePermanentHealthBonusDoesNotStack()
    {
        _health.SetPermanentHealthBonus(25);
        _health.SetPermanentHealthBonus(25);

        Assert.AreEqual(125, _health.MaxHealth);
        Assert.AreEqual(125, _health.CurrentHealth);
    }

    [Test]
    public void IncreasingPermanentHealthBonusUpdatesTotal()
    {
        _health.SetPermanentHealthBonus(20);

        Assert.AreEqual(120, _health.MaxHealth);

        _health.SetPermanentHealthBonus(40);

        Assert.AreEqual(140, _health.MaxHealth);
        Assert.AreEqual(140, _health.CurrentHealth);
    }

    [Test]
    public void DecreasingPermanentHealthBonusPreservesMissingHealth()
    {
        _health.SetPermanentHealthBonus(40); // 140/140
        _health.TakeDamage(40);              // 100/140

        _health.SetPermanentHealthBonus(20);

        Assert.AreEqual(120, _health.MaxHealth);
        Assert.AreEqual(80, _health.CurrentHealth);
    }

    [Test]
    public void SettingPermanentHealthBonusToZeroRestoresDefaultMax()
    {
        _health.SetPermanentHealthBonus(50);

        Assert.AreEqual(150, _health.MaxHealth);

        _health.SetPermanentHealthBonus(0);

        Assert.AreEqual(100, _health.MaxHealth);
        Assert.AreEqual(100, _health.CurrentHealth);
    }

    [Test]
    public void NegativePermanentHealthBonusIsClampedToZero()
    {
        _health.SetPermanentHealthBonus(-25);

        Assert.AreEqual(100, _health.MaxHealth);
        Assert.AreEqual(100, _health.CurrentHealth);
    }

    [Test]
    public void NegativePermanentHealthBonusRemovesExistingBonus()
    {
        _health.SetPermanentHealthBonus(30);
        _health.SetPermanentHealthBonus(-10);

        Assert.AreEqual(100, _health.MaxHealth);
        Assert.AreEqual(100, _health.CurrentHealth);
    }

    [Test]
    public void PermanentHealthBonusDoesNotReviveDeadPlayer()
    {
        _health.TakeDamage(100);

        Assert.IsTrue(_health.IsDead);

        _health.SetPermanentHealthBonus(50);

        Assert.AreEqual(150, _health.MaxHealth);
        Assert.AreEqual(0, _health.CurrentHealth);
        Assert.IsTrue(_health.IsDead);
    }

    [Test]
    public void PermanentHealthChangePublishesHealthChangedEvent()
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

        _health.SetPermanentHealthBonus(25);

        Assert.AreEqual(1, eventCount);
        Assert.AreEqual(125, reportedCurrent);
        Assert.AreEqual(125, reportedMax);
    }

    [Test]
    public void ReapplyingSameBonusDoesNotPublishAnotherEvent()
    {
        _health.SetPermanentHealthBonus(25);

        int eventCount = 0;

        _health.OnHealthChanged += (current, max) =>
            eventCount++;

        _health.SetPermanentHealthBonus(25);

        Assert.AreEqual(0, eventCount);
    }

    // RESET HEALTH

    [Test]
    public void ResetHealthRestoresFullHealth()
    {
        _health.TakeDamage(75);
        _health.ResetHealth();

        Assert.AreEqual(100, _health.CurrentHealth);
        Assert.IsFalse(_health.IsDead);
    }

    [Test]
    public void ResetHealthPreservesPermanentHealthBonus()
    {
        _health.SetPermanentHealthBonus(50);
        _health.TakeDamage(80);
        _health.ResetHealth();

        Assert.AreEqual(150, _health.MaxHealth);
        Assert.AreEqual(150, _health.CurrentHealth);
    }

    [Test]
    public void ResetHealthPublishesHealthChangedEvent()
    {
        _health.TakeDamage(50);

        int eventCount = 0;
        int reportedCurrent = -1;
        int reportedMax = -1;

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
    public void ResetHealthAfterDeathRestoresPlayer()
    {
        _health.TakeDamage(100);

        Assert.IsTrue(_health.IsDead);

        _health.ResetHealth();

        Assert.AreEqual(100, _health.CurrentHealth);
        Assert.IsFalse(_health.IsDead);
    }

    // CONTRACT

    [Test]
    public void HealthContractExposesHealthChangesAndDeath()
    {
        IPlayerHealth health = _health;

        int healthChangeCount = 0;
        int deathCount = 0;
        int reportedCurrent = -1;
        int reportedMax = -1;

        health.OnHealthChanged += (current, max) =>
        {
            healthChangeCount++;
            reportedCurrent = current;
            reportedMax = max;
        };

        health.OnPlayerDied += () => deathCount++;

        health.TakeDamage(100);

        Assert.AreEqual(0, health.CurrentHealth);
        Assert.AreEqual(100, health.MaxHealth);
        Assert.AreEqual(1, healthChangeCount);
        Assert.AreEqual(0, reportedCurrent);
        Assert.AreEqual(100, reportedMax);
        Assert.AreEqual(1, deathCount);
    }
}
