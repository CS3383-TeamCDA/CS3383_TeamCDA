using UnityEngine;

public interface IPlayerMovementInput
{
    Vector2 Movement { get; }
    bool JumpJustPressed { get; }
    bool JumpBeingHeld { get; }
    bool JumpReleased { get; }
}
