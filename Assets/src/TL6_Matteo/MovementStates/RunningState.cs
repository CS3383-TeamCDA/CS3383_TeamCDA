
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

        TempPlayerController player = manager.Player;

        if (player.Input.JumpJustPressed && player.IsGrounded())
        {
            manager.ChangeState(new JumpingState());
        }
    }

    public override void FixedTick(PlayerStateManager manager)
    {
        MoveHorizontally(manager.Player);
    }

    // Shared with JumpingState so air movement still works.
    public static void MoveHorizontally(TempPlayerController player)
    {
        Rigidbody2D rb = player.Rb;

        float inputX = Mathf.Clamp(
            player.Input.Movement.x, -1f, 1f);

        float targetSpeed = inputX * player.MaxSpeedX;

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
}
