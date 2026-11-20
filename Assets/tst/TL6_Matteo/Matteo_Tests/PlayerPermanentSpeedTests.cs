
using System;
using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.Contracts;

public class PlayerPermanentSpeedTests
{
    private GameObject _player;
    private PlayerControllerTL6 _controller;
    private FakeMetaProgressionManager _progression;

    [SetUp]
    public void SetUp()
    {
        _player = new GameObject("TestPlayer");

        _player.AddComponent<Rigidbody2D>();
        _player.AddComponent<BoxCollider2D>();

        _controller = _player.AddComponent<PlayerControllerTL6>();

        _progression = new FakeMetaProgressionManager();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(_player);
    }

    // DEFAULT SPEED

    [Test]
    public void PlayerStartsWithDefaultSpeed()
    {
        Assert.AreEqual(8f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void MissingProgressionUsesDefaultSpeed()
    {
        // No progression manager injected.
        Assert.AreEqual(8f, _controller.MaxSpeedX, 0.001f);

        _controller.SetTemporarySpeedMultiplier(1.25f);

        Assert.AreEqual(10f, _controller.MaxSpeedX, 0.001f);
    }

    // PERMANENT UPGRADES

    [Test]
    public void PermanentUpgradeIncreasesSpeed()
    {
        _progression.SpeedBonus = 2f;
        _controller.Inject(_progression);

        Assert.AreEqual(10f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void MultiplePermanentUpgradesIncreaseSpeed()
    {
        _controller.Inject(_progression);

        _progression.SpeedBonus = 2f;
        Assert.AreEqual(10f, _controller.MaxSpeedX, 0.001f);

        _progression.SpeedBonus = 4f;
        Assert.AreEqual(12f, _controller.MaxSpeedX, 0.001f);

        _progression.SpeedBonus = 6f;
        Assert.AreEqual(14f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void ProgressionChangesUpdateSpeedImmediately()
    {
        _progression.SpeedBonus = 1f;
        _controller.Inject(_progression);

        Assert.AreEqual(9f, _controller.MaxSpeedX, 0.001f);

        _progression.SpeedBonus = 5f;

        Assert.AreEqual(13f, _controller.MaxSpeedX, 0.001f);
    }

    // TEMPORARY MULTIPLIERS

    [Test]
    public void TemporaryMultiplierIncreasesSpeed()
    {
        _controller.SetTemporarySpeedMultiplier(1.5f);

        Assert.AreEqual(12f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void TemporaryMultiplierCanSlowPlayer()
    {
        _controller.SetTemporarySpeedMultiplier(0.5f);

        Assert.AreEqual(4f, _controller.MaxSpeedX, 0.001f);

        _controller.ResetTemporarySpeedMultiplier();

        Assert.AreEqual(8f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void NegativeMultiplierIsClampedToZero()
    {
        _controller.SetTemporarySpeedMultiplier(-2f);

        Assert.AreEqual(0f, _controller.MaxSpeedX, 0.001f);
    }

    // PERMANENT AND TEMPORARY COMBINED

    [Test]
    public void ResetTemporaryMultiplierPreservesPermanentUpgrade()
    {
        _progression.SpeedBonus = 2f;
        _controller.Inject(_progression);

        _controller.SetTemporarySpeedMultiplier(1.5f);

        Assert.AreEqual(15f, _controller.MaxSpeedX, 0.001f);

        _controller.ResetTemporarySpeedMultiplier();

        Assert.AreEqual(10f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void PermanentUpgradeAppliesDuringTemporaryMultiplier()
    {
        _controller.Inject(_progression);

        _controller.SetTemporarySpeedMultiplier(1.5f);

        Assert.AreEqual(12f, _controller.MaxSpeedX, 0.001f);

        _progression.SpeedBonus = 2f;

        Assert.AreEqual(15f, _controller.MaxSpeedX, 0.001f);

        _progression.SpeedBonus = 4f;

        Assert.AreEqual(18f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void NegativeTotalSpeedIsClampedToZero()
    {
        _progression.SpeedBonus = -20f;
        _controller.Inject(_progression);

        Assert.AreEqual(0f, _controller.MaxSpeedX, 0.001f);

        _controller.SetTemporarySpeedMultiplier(1.5f);

        Assert.AreEqual(0f, _controller.MaxSpeedX, 0.001f);
    }
}

// Test-only implementation of TL5's progression contract.
public class FakeMetaProgressionManager : IMetaProgressionManager
{
    public int Currency { get; private set; }

    public float SpeedBonus { get; set; }

    public int SpeedLevel { get; set; }

    public event Action<int> OnCurrencyChanged;

    public int GetUpgradeLevel(UpgradeType type)
    {
        return type == UpgradeType.Speed ? SpeedLevel : 0;
    }

    public float GetEffectValue(UpgradeType type)
    {
        return type == UpgradeType.Speed ? SpeedBonus : 0f;
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
            return;

        Currency += amount;
        OnCurrencyChanged?.Invoke(Currency);
    }
}
