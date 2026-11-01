
using UnityEngine;
using StateLogic = Assets.src.StateLogic;

public class JumpingState : MovementState
{
    public override void EnterState(
        StateLogic.StateManager stateManager)
    {
        PlayerStateManager manager =
            (PlayerStateManager)stateManager;

        TempPlayerController player = manager.Player;

        // Apply the initial jump velocity.
        player.Rb.linearVelocity = new Vector2(
            player.Rb.linearVelocity.x,
            player.JumpSpeed
        );
    }

    public override void UpdateState(
        StateLogic.StateManager stateManager)
    {
        PlayerStateManager manager =
            (PlayerStateManager)stateManager;

        TempPlayerController player = manager.Player;

        // Return to running when the player lands.
        if (player.Rb.linearVelocity.y <= 0f &&
            player.IsGrounded())
        {
            manager.ChangeState(new RunningState());
        }
    }

    public override void FixedTick(PlayerStateManager manager)
    {
        // Allow horizontal movement while airborne.
        RunningState.MoveHorizontally(manager.Player);
    }
}
