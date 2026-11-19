using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerCombatInputHandler : MonoBehaviour, IPlayerCombatInput
{
    private PlayerInput _playerInput;

    private InputAction _fireAction;
    private InputAction _aimPointerAction;

    public Vector2 AimScreenPosition { get; private set; }
    public bool FireHeld { get; private set; }

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        _fireAction = _playerInput.actions.FindAction("Fire");
        _aimPointerAction = _playerInput.actions.FindAction("AimPointer");
    }

    private void Update()
    {
        if (_fireAction != null)
            FireHeld = _fireAction.IsPressed();

        if (_aimPointerAction != null)
            AimScreenPosition = _aimPointerAction.ReadValue<Vector2>();

    }

}
