using NUnit.Framework;
using UnityEngine;

public class PlayerPermanentSpeedTests
{
    private GameObject _player;
    private PlayerControllerTL6 _controller;

    [SetUp]
    public void SetUp()
    {
        _player = new GameObject("TestPlayer");

        _player.AddComponent<Rigidbody2D>();
        _player.AddComponent<BoxCollider2D>();

        _controller = _player.AddComponent<PlayerControllerTL6>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_player);
    }

    // DEFAULT SPEED

    [Test]
    public void PlayerStartsWithDefaultSpeed()
    {
        Assert.AreEqual(8f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void PlayerWithoutPermanentUpgradeUsesDefaultSpeed()
    {
        Assert.AreEqual(8f, _controller.MaxSpeedX, 0.001f);

        _controller.SetTemporarySpeedMultiplier(1.25f);

        Assert.AreEqual(10f, _controller.MaxSpeedX, 0.001f);
    }

    // PERMANENT UPGRADES

    [Test]
    public void PermanentUpgradeIncreasesSpeed()
    {
        _controller.SetPermanentSpeedBonus(2f);

        Assert.AreEqual(10f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void MultiplePermanentUpgradeLevelsIncreaseSpeed()
    {
        _controller.SetPermanentSpeedBonus(2f);
        Assert.AreEqual(10f, _controller.MaxSpeedX, 0.001f);

        _controller.SetPermanentSpeedBonus(4f);
        Assert.AreEqual(12f, _controller.MaxSpeedX, 0.001f);

        _controller.SetPermanentSpeedBonus(6f);
        Assert.AreEqual(14f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void PermanentUpgradeChangesUpdateSpeedImmediately()
    {
        _controller.SetPermanentSpeedBonus(1f);

        Assert.AreEqual(9f, _controller.MaxSpeedX, 0.001f);

        _controller.SetPermanentSpeedBonus(5f);

        Assert.AreEqual(13f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void ApplyingSamePermanentBonusTwiceDoesNotStack()
    {
        _controller.SetPermanentSpeedBonus(2f);
        _controller.SetPermanentSpeedBonus(2f);

        Assert.AreEqual(10f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void NegativePermanentBonusIsClampedToZero()
    {
        _controller.SetPermanentSpeedBonus(-20f);

        Assert.AreEqual(8f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void SettingPermanentBonusToZeroRestoresBaseSpeed()
    {
        _controller.SetPermanentSpeedBonus(4f);

        Assert.AreEqual(12f, _controller.MaxSpeedX, 0.001f);

        _controller.SetPermanentSpeedBonus(0f);

        Assert.AreEqual(8f, _controller.MaxSpeedX, 0.001f);
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
        _controller.SetPermanentSpeedBonus(2f);

        _controller.SetTemporarySpeedMultiplier(1.5f);

        Assert.AreEqual(15f, _controller.MaxSpeedX, 0.001f);

        _controller.ResetTemporarySpeedMultiplier();

        Assert.AreEqual(10f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void PermanentUpgradeAppliesDuringTemporaryMultiplier()
    {
        _controller.SetTemporarySpeedMultiplier(1.5f);

        Assert.AreEqual(12f, _controller.MaxSpeedX, 0.001f);

        _controller.SetPermanentSpeedBonus(2f);

        Assert.AreEqual(15f, _controller.MaxSpeedX, 0.001f);

        _controller.SetPermanentSpeedBonus(4f);

        Assert.AreEqual(18f, _controller.MaxSpeedX, 0.001f);
    }

    [Test]
    public void ResettingPermanentBonusPreservesTemporaryMultiplier()
    {
        _controller.SetPermanentSpeedBonus(2f);
        _controller.SetTemporarySpeedMultiplier(1.5f);

        Assert.AreEqual(15f, _controller.MaxSpeedX, 0.001f);

        _controller.SetPermanentSpeedBonus(0f);

        Assert.AreEqual(12f, _controller.MaxSpeedX, 0.001f);
    }
}
