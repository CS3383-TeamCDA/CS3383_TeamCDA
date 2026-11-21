using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.Contracts;

public class PlayerPermanentUpgradeReceiverTests
{
    private GameObject _player;
    private PlayerControllerTL6 _movementController;
    private PlayerCombatController _combatController;
    private IPlayerPermanentUpgradeReceiver _receiver;

    [SetUp]
    public void SetUp()
    {
        _player = new GameObject("TestPlayer");

        _player.AddComponent<Rigidbody2D>();
        _player.AddComponent<BoxCollider2D>();

        _movementController =
            _player.AddComponent<PlayerControllerTL6>();

        _combatController =
            _player.AddComponent<PlayerCombatController>();

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

        Assert.AreEqual(
            5,
            GetAttackBonus()
        );
    }

    [Test]
    public void ReceiverDoesNotStackRepeatedAttackBonuses()
    {
        _receiver.SetPermanentAttackBonus(3f);
        _receiver.SetPermanentAttackBonus(3f);

        Assert.AreEqual(
            3,
            GetAttackBonus()
        );

        _receiver.SetPermanentAttackBonus(5f);

        Assert.AreEqual(
            5,
            GetAttackBonus()
        );
    }

    // UPGRADE INDEPENDENCE

    [Test]
    public void ReceiverKeepsSpeedAndAttackIndependent()
    {
        _receiver.SetPermanentSpeedBonus(2f);
        _receiver.SetPermanentAttackBonus(5f);

        Assert.AreEqual(
            10f,
            _movementController.MaxSpeedX,
            0.001f
        );

        Assert.AreEqual(
            5,
            GetAttackBonus()
        );

        // Update speed without affecting attack.
        _receiver.SetPermanentSpeedBonus(4f);

        Assert.AreEqual(
            12f,
            _movementController.MaxSpeedX,
            0.001f
        );

        Assert.AreEqual(
            5,
            GetAttackBonus()
        );

        // Update attack without affecting speed.
        _receiver.SetPermanentAttackBonus(8f);

        Assert.AreEqual(
            12f,
            _movementController.MaxSpeedX,
            0.001f
        );

        Assert.AreEqual(
            8,
            GetAttackBonus()
        );
    }

    // TEST HELPER

    private int GetAttackBonus()
    {
        FieldInfo field = typeof(PlayerCombatController).GetField(
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
