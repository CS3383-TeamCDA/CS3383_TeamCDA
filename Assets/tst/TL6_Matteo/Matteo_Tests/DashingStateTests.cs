
using NUnit.Framework;
using UnityEngine;

public class DashingStateTests
{
    private GameObject _player;
    private PlayerControllerTL6 _controller;
    private PlayerStateManager _manager;
    private Rigidbody2D _rb;

    [SetUp]
    public void SetUp()
    {
        _player = new GameObject("TestPlayer");

        _rb = _player.AddComponent<Rigidbody2D>();
        _rb.gravityScale = 1f;

        _player.AddComponent<BoxCollider2D>();

        _controller = _player.AddComponent<PlayerControllerTL6>();
        _manager = new PlayerStateManager(_controller);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_player);
    }

    [Test]
    public void AirDashStartsAvailable()
    {
        Assert.IsTrue(_controller.AirDashAvailable);
    }

    [Test]
    public void ConsumingAirDashMakesItUnavailable()
    {
        _controller.ConsumeAirDash();

        Assert.IsFalse(_controller.AirDashAvailable);
    }

    [Test]
    public void ResettingAirDashMakesItAvailableAgain()
    {
        _controller.ConsumeAirDash();
        _controller.ResetAirDash();

        Assert.IsTrue(_controller.AirDashAvailable);
    }

    [Test]
    public void AirDashRemainsUnavailableUntilReset()
    {
        _controller.ConsumeAirDash();

        Assert.IsFalse(_controller.AirDashAvailable);
        Assert.IsFalse(_controller.AirDashAvailable);

        _controller.ResetAirDash();

        Assert.IsTrue(_controller.AirDashAvailable);
    }

    [Test]
    public void DashTemporarilyDisablesGravity()
    {
        var dash = new DashingState();

        dash.EnterState(_manager);

        Assert.AreEqual(0f, _rb.gravityScale);
    }

    [Test]
    public void DashRestoresGravityOnExit()
    {
        float originalGravity = _rb.gravityScale;

        var dash = new DashingState();
        dash.EnterState(_manager);
        dash.ExitState(_manager);

        Assert.AreEqual(originalGravity, _rb.gravityScale);
    }

    [Test]
    public void DashSetsHorizontalVelocity()
    {
        var dash = new DashingState();

        dash.EnterState(_manager);

        Assert.AreEqual(
            _controller.DashSpeed,
            Mathf.Abs(_rb.linearVelocity.x),
            0.001f
        );
    }

    [Test]
    public void DashStartsWithZeroVerticalVelocity()
    {
        _rb.linearVelocity = new Vector2(0f, 5f);

        var dash = new DashingState();
        dash.EnterState(_manager);

        Assert.AreEqual(0f, _rb.linearVelocity.y, 0.001f);
    }


    [Test]
    public void FacingRightDashesRight()
    {
        _controller.UpdateFacingDirection(1f);

        var dash = new DashingState();
        dash.EnterState(_manager);

        Assert.Greater(_rb.linearVelocity.x, 0f);
    }

    [Test]
    public void FacingLeftDashesLeft()
    {
        _controller.UpdateFacingDirection(-1f);

        var dash = new DashingState();
        dash.EnterState(_manager);

        Assert.Less(_rb.linearVelocity.x, 0f);
    }

    [Test]
    public void ZeroInputPreservesFacingDirection()
    {
        _controller.UpdateFacingDirection(-1f);
        _controller.UpdateFacingDirection(0f);

        Assert.AreEqual(-1, _controller.FacingDirection);
    }


}
