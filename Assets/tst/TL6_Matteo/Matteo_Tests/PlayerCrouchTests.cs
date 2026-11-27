using NUnit.Framework;
using UnityEngine;

public class PlayerCrouchTests
{
    private GameObject _player;
    private PlayerControllerTL6 _controller;
    private CapsuleCollider2D _collider;

    [SetUp]
    public void SetUp()
    {
        _player = new GameObject("TestPlayer");

        _player.AddComponent<Rigidbody2D>();

        _collider = _player.AddComponent<CapsuleCollider2D>();
        _collider.size = new Vector2(0.5f, 1f);
        _collider.offset = Vector2.zero;

        _controller = _player.AddComponent<PlayerControllerTL6>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_player);
    }

    [Test]
    public void PlayerStartsStanding()
    {
        Assert.IsFalse(_controller.IsCrouching);
    }

    [Test]
    public void CrouchingUpdatesPlayerState()
    {
        _controller.SetCrouching(true);

        Assert.IsTrue(_controller.IsCrouching);
    }

    [Test]
    public void StandingResetsCrouchingState()
    {
        _controller.SetCrouching(true);
        _controller.SetCrouching(false);

        Assert.IsFalse(_controller.IsCrouching);
    }

    [Test]
    public void CrouchingReducesColliderHeight()
    {
        float originalHeight = _collider.size.y;

        _controller.SetCrouching(true);

        Assert.Less(_collider.size.y, originalHeight);
    }

    [Test]
    public void CrouchingPreservesColliderWidth()
    {
        float originalWidth = _collider.size.x;

        _controller.SetCrouching(true);

        Assert.AreEqual(originalWidth, _collider.size.x);
    }

    [Test]
    public void CrouchingPreservesColliderBottom()
    {
        float originalBottom =
            _collider.offset.y - _collider.size.y / 2f;

        _controller.SetCrouching(true);

        float crouchingBottom =
            _collider.offset.y - _collider.size.y / 2f;

        Assert.AreEqual(
            originalBottom, crouchingBottom, 0.0001f);
    }

    [Test]
    public void StandingRestoresCollider()
    {
        Vector2 originalSize = _collider.size;
        Vector2 originalOffset = _collider.offset;

        _controller.SetCrouching(true);
        _controller.SetCrouching(false);

        Assert.AreEqual(originalSize, _collider.size);
        Assert.AreEqual(originalOffset, _collider.offset);
    }

    [Test]
    public void RepeatedCrouchingDoesNotShrinkColliderFurther()
    {
        _controller.SetCrouching(true);

        Vector2 crouchingSize = _collider.size;
        Vector2 crouchingOffset = _collider.offset;

        _controller.SetCrouching(true);

        Assert.AreEqual(crouchingSize, _collider.size);
        Assert.AreEqual(crouchingOffset, _collider.offset);
    }

    [Test]
    public void RepeatedCrouchCyclesRestoreCollider()
    {
        Vector2 originalSize = _collider.size;
        Vector2 originalOffset = _collider.offset;

        for (int i = 0; i < 10; i++)
        {
            _controller.SetCrouching(true);
            _controller.SetCrouching(false);
        }

        Assert.AreEqual(originalSize, _collider.size);
        Assert.AreEqual(originalOffset, _collider.offset);
    }

    [Test]
    public void CrouchSpeedMultiplierReducesTargetSpeed()
    {
        float runningSpeed = _controller.MaxSpeedX;

        float crouchSpeed =
            runningSpeed * _controller.CrouchSpeedMultiplier;

        Assert.Less(crouchSpeed, runningSpeed);
    }

    [Test]
    public void CrouchTargetSpeedRespectsPermanentSpeedBonus()
    {
        float originalCrouchSpeed =
            _controller.MaxSpeedX *
            _controller.CrouchSpeedMultiplier;

        _controller.SetPermanentSpeedBonus(2f);

        float boostedCrouchSpeed =
            _controller.MaxSpeedX *
            _controller.CrouchSpeedMultiplier;

        Assert.AreEqual(
            originalCrouchSpeed +
            2f * _controller.CrouchSpeedMultiplier,
            boostedCrouchSpeed,
            0.0001f);
    }

    [Test]
    public void CrouchTargetSpeedRespectsTemporarySpeedMultiplier()
    {
        float originalCrouchSpeed =
            _controller.MaxSpeedX *
            _controller.CrouchSpeedMultiplier;

        _controller.SetTemporarySpeedMultiplier(1.5f);

        float boostedCrouchSpeed =
            _controller.MaxSpeedX *
            _controller.CrouchSpeedMultiplier;

        Assert.AreEqual(
            originalCrouchSpeed * 1.5f,
            boostedCrouchSpeed,
            0.0001f);
    }
}