using UnityEngine;

public interface IPlayerCombatInput
{
    Vector2 AimScreenPosition { get; }
    bool FireHeld { get; }
}
