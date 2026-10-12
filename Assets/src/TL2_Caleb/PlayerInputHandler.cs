using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 Movement {get; private set;}
    public bool JumpJustPressed {get; private set;}
    public bool JumpBeingHeld {get; private set;}
    public bool JumpReleased {get; private set;}

    [SerializeField] private PlayerInput _playerInput;

    private InputAction _moveAction;
    private InputAction _jumpAction;

    private void Awake()
    {
        SetupInputActions();
    }

    private void Update()
    {
        UpdateInputs();       
    }

    private void SetupInputActions()
    {
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];
    }

    private void UpdateInputs()
    {
        Movement = _moveAction.ReadValue<Vector2>();
        JumpJustPressed = _jumpAction.WasPressedThisFrame();
        JumpBeingHeld = _jumpAction.IsPressed();
        JumpReleased = _jumpAction.WasReleasedThisFrame();
    }
}
