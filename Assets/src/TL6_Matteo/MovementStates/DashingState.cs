
using UnityEngine;
using StateLogic = Assets.src.StateLogic;

public class DashingState : MovementState
{
    private float _dashTimer;
    private float _dashDirection;
    private float _originalGravityScale;

    public override void EnterState(
        StateLogic.StateManager stateManager)
    {
        PlayerStateManager manager =
            (PlayerStateManager)stateManager;

        PlayerControllerTL6 player = manager.Player;
        Rigidbody2D rb = player.Rb;

        // Reset the dash timer.
        _dashTimer = 0f;

        // Save gravity so we can restore it afterward.
        _originalGravityScale = rb.gravityScale;

        // Choose a direction based on player input.
        _dashDirection = player.FacingDirection;

        if (player.DisableGravityDuringDash)
        {
            rb.gravityScale = 0f;
        }

        // Immediately begin the dash.
        rb.linearVelocity = new Vector2(
            _dashDirection * player.DashSpeed,
            player.DisableGravityDuringDash
                ? 0f
                : rb.linearVelocity.y
        );
    }

    public override void UpdateState(
        StateLogic.StateManager stateManager)
    {
        // No frame-based logic needed yet.
    }

    public override void FixedTick(PlayerStateManager manager)
    {
        PlayerControllerTL6 player = manager.Player;
        Rigidbody2D rb = player.Rb;

        // Maintain a constant horizontal dash velocity.
        rb.linearVelocity = new Vector2(
            _dashDirection * player.DashSpeed,
            player.DisableGravityDuringDash
                ? 0f
                : rb.linearVelocity.y
        );

        // Advance the timer using the physics timestep.
        _dashTimer += Time.fixedDeltaTime;

        if (_dashTimer >= player.DashDuration)
        {
            if (player.IsGrounded())
            {
                manager.ChangeState(new RunningState());
            }
            else
            {
                // Enter the airborne state without jumping again.
                manager.ChangeState(new JumpingState(false));
            }
        }
    }

    public override void ExitState(
        StateLogic.StateManager stateManager)
    {
        PlayerStateManager manager =
            (PlayerStateManager)stateManager;

        // Restore the player's original gravity.
        manager.Player.Rb.gravityScale = _originalGravityScale;
    }
}
