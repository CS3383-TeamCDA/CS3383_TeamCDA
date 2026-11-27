
using UnityEngine;
using StateLogic = Assets.src.StateLogic;

public class RunningState : MovementState
{

    public override string Execute()
    {
        return "RunningState: Execute";
    }

    public override void UpdateState(
        StateLogic.StateManager stateManager)
    {
        PlayerStateManager manager =
            (PlayerStateManager)stateManager;

        PlayerControllerTL6 player = manager.Player;

        player.UpdateFacingDirection(player.Input.Movement.x);

        if (player.Input.CrouchBeingHeld && player.IsGrounded())
        {
            manager.ChangeState(new CrouchingState());
            return;
        }

        if (player.Input.JumpJustPressed && player.IsGrounded())
        {
            manager.ChangeState(new JumpingState());
            return;
        }

        if (player.Input.DashJustPressed)
        {
            manager.ChangeState(new DashingState());
            return;
        }
    }

    public override void FixedTick(PlayerStateManager manager)
    {
        MoveHorizontally(manager.Player);
    }

    // Shared with JumpingState so air movement still works.
    public static void MoveHorizontally(
        PlayerControllerTL6 player,
        float speedMultiplier = 1f)
    {
        Rigidbody2D rb = player.Rb;

        float inputX = Mathf.Clamp(
            player.Input.Movement.x, -1f, 1f);

        float targetSpeed =
            inputX * player.MaxSpeedX * speedMultiplier;

        float rate = Mathf.Approximately(inputX, 0f)
            ? player.XDeceleration
            : player.XAcceleration;

        float newX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(
            newX, rb.linearVelocity.y);
    }

    public override void EnterState(StateLogic.StateManager stateManager)
    {
        PlayerStateManager manager =
            (PlayerStateManager)stateManager;

        manager.Player.Rb.gravityScale =
            manager.Player.BaseGravityScale;
    }
}
