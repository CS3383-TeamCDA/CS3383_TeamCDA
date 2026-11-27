using UnityEngine;
using StateLogic = Assets.src.StateLogic;

public class CrouchingState : MovementState
{
    public override void EnterState(
        StateLogic.StateManager stateManager)
    {
        PlayerStateManager manager =
            (PlayerStateManager)stateManager;

        PlayerControllerTL6 player = manager.Player;

        player.SetCrouching(true);
    }

    public override void UpdateState(
        StateLogic.StateManager stateManager)
    {
        PlayerStateManager manager =
            (PlayerStateManager)stateManager;

        PlayerControllerTL6 player = manager.Player;
        player.UpdateFacingDirection(player.Input.Movement.x);

        if (!player.IsGrounded())
        {
            manager.ChangeState(new JumpingState(false));
            return;
        }

        if (!player.Input.CrouchBeingHeld)
        {
            manager.ChangeState(new RunningState());
            return;
        }
    }

    public override void FixedTick(PlayerStateManager manager)
    {
        PlayerControllerTL6 player = manager.Player;

        RunningState.MoveHorizontally(
            player, player.CrouchSpeedMultiplier);
    }

    public override void ExitState(
        StateLogic.StateManager stateManager)
    {
        PlayerStateManager manager =
            (PlayerStateManager)stateManager;

        manager.Player.SetCrouching(false);
    }
}