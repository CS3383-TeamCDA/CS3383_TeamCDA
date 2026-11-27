
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.Contracts;

public class PlayerPermanentUpgradeReceiverTests
{
    private GameObject _player;
    private PlayerControllerTL6 _movementController;
    private PlayerCombatController _combatController;
    private PlayerHealth _health;
    private IPlayerPermanentUpgradeReceiver _receiver;

    [SetUp]
    public void SetUp()
    {
        _player = new GameObject("TestPlayer");

        _player.AddComponent<Rigidbody2D>();
        _player.AddComponent<CapsuleCollider2D>();

        _movementController =
            _player.AddComponent<PlayerControllerTL6>();

        _combatController =
            _player.AddComponent<PlayerCombatController>();

        _health =
            _player.AddComponent<PlayerHealth>();

        _receiver =
            _player.AddComponent<PlayerPermanentUpgradeReceiver>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_player);
    }

    // SPEED UPGRADES

    [Test]
    public void ReceiverAppliesPermanentSpeedBonus()
    {
        _receiver.SetPermanentSpeedBonus(2f);

        Assert.AreEqual(
            10f,
            _movementController.MaxSpeedX,
            0.001f
        );
    }

    [Test]
    public void ReceiverPreservesTemporaryMultiplier()
    {
        _movementController.SetTemporarySpeedMultiplier(1.5f);

        Assert.AreEqual(
            12f,
            _movementController.MaxSpeedX,
            0.001f
        );

        _receiver.SetPermanentSpeedBonus(2f);

        Assert.AreEqual(
            15f,
            _movementController.MaxSpeedX,
            0.001f
        );

        _movementController.ResetTemporarySpeedMultiplier();

        Assert.AreEqual(
            10f,
            _movementController.MaxSpeedX,
            0.001f
        );
    }

    [Test]
    public void ReceiverDoesNotStackRepeatedSpeedBonuses()
    {
        _receiver.SetPermanentSpeedBonus(2f);
        _receiver.SetPermanentSpeedBonus(2f);

        Assert.AreEqual(
            10f,
            _movementController.MaxSpeedX,
            0.001f
        );
    }

    // ATTACK UPGRADES

    [Test]
    public void ReceiverUpdatesAttackBonus()
    {
        _receiver.SetPermanentAttackBonus(5f);

        Assert.AreEqual(5, GetAttackBonus());
    }

    [Test]
    public void ReceiverDoesNotStackRepeatedAttackBonuses()
    {
        _receiver.SetPermanentAttackBonus(3f);
        _receiver.SetPermanentAttackBonus(3f);

        Assert.AreEqual(3, GetAttackBonus());

        _receiver.SetPermanentAttackBonus(5f);

        Assert.AreEqual(5, GetAttackBonus());
    }

    // HEALTH UPGRADES

    [Test]
    public void ReceiverAppliesPermanentHealthBonus()
    {
        _receiver.SetPermanentHealthBonus(25f);

        Assert.AreEqual(125, _health.MaxHealth);
        Assert.AreEqual(125, _health.CurrentHealth);
    }

    [Test]
    public void ReceiverDoesNotStackRepeatedHealthBonuses()
    {
        _receiver.SetPermanentHealthBonus(25f);
        _receiver.SetPermanentHealthBonus(25f);

        Assert.AreEqual(125, _health.MaxHealth);
        Assert.AreEqual(125, _health.CurrentHealth);
    }

    [Test]
    public void ReceiverPreservesMissingHealthWhenUpgrading()
    {
        _health.TakeDamage(40); // 60/100

        _receiver.SetPermanentHealthBonus(25f);

        Assert.AreEqual(125, _health.MaxHealth);
        Assert.AreEqual(85, _health.CurrentHealth);
    }

    [Test]
    public void ReceiverCanUpdateHealthBonus()
    {
        _receiver.SetPermanentHealthBonus(20f);

        Assert.AreEqual(120, _health.MaxHealth);

        _receiver.SetPermanentHealthBonus(40f);

        Assert.AreEqual(140, _health.MaxHealth);
        Assert.AreEqual(140, _health.CurrentHealth);
    }

    [Test]
    public void ReceiverHealthBonusDoesNotRevivePlayer()
    {
        _health.TakeDamage(100);

        _receiver.SetPermanentHealthBonus(50f);

        Assert.AreEqual(150, _health.MaxHealth);
        Assert.AreEqual(0, _health.CurrentHealth);
        Assert.IsTrue(_health.IsDead);
    }

    [Test]
    public void ReceiverRoundsHealthBonusToWholeNumber()
    {
        _receiver.SetPermanentHealthBonus(2.6f);

        Assert.AreEqual(103, _health.MaxHealth);
        Assert.AreEqual(103, _health.CurrentHealth);
    }

    // UPGRADE INDEPENDENCE

    [Test]
    public void ReceiverKeepsAllUpgradesIndependent()
    {
        _receiver.SetPermanentSpeedBonus(2f);
        _receiver.SetPermanentAttackBonus(5f);
        _receiver.SetPermanentHealthBonus(25f);

        Assert.AreEqual(
            10f,
            _movementController.MaxSpeedX,
            0.001f
        );

        Assert.AreEqual(5, GetAttackBonus());
        Assert.AreEqual(125, _health.MaxHealth);

        // Change speed only.
        _receiver.SetPermanentSpeedBonus(4f);

        Assert.AreEqual(
            12f,
            _movementController.MaxSpeedX,
            0.001f
        );

        Assert.AreEqual(5, GetAttackBonus());
        Assert.AreEqual(125, _health.MaxHealth);

        // Change attack only.
        _receiver.SetPermanentAttackBonus(8f);

        Assert.AreEqual(
            12f,
            _movementController.MaxSpeedX,
            0.001f
        );

        Assert.AreEqual(8, GetAttackBonus());
        Assert.AreEqual(125, _health.MaxHealth);

        // Change health only.
        _receiver.SetPermanentHealthBonus(50f);

        Assert.AreEqual(
            12f,
            _movementController.MaxSpeedX,
            0.001f
        );

        Assert.AreEqual(8, GetAttackBonus());
        Assert.AreEqual(150, _health.MaxHealth);
    }

    // TEST HELPER

    private int GetAttackBonus()
    {
        FieldInfo field =
            typeof(PlayerCombatController).GetField(
                "_permanentAttackBonus",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        Assert.IsNotNull(
            field,
            "Could not find _permanentAttackBonus in PlayerCombatController."
        );

        return (int)field.GetValue(_combatController);
    }
}
