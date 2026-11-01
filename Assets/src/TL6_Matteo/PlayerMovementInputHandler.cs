using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementInputHandler : MonoBehaviour, IPlayerMovementInput
{
    [SerializeField] private PlayerInput _playerInput;

    private InputAction _moveAction;
    private InputAction _jumpAction;

    public Vector2 Movement { get; private set; }
    public bool JumpJustPressed { get; private set; }
    public bool JumpBeingHeld { get; private set; }
    public bool JumpReleased { get; private set; }



    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        _moveAction = _playerInput.actions.FindAction("Move");
        _jumpAction = _playerInput.actions.FindAction("Jump");
    }

    private void Update()
    {
        Movement = _moveAction.ReadValue<Vector2>();
        JumpJustPressed = _jumpAction.WasPressedThisFrame();
    }

    private void CacheInputActions()
    {
        if (_moveAction != null && _jumpAction != null) return;

        if (_playerInput != null && _playerInput.actions != null)
        {
            _moveAction = _playerInput.actions.FindAction("Move");
            _jumpAction = _playerInput.actions.FindAction("Jump");
        }
    }

    private void UpdateInputs()
    {
        if (_moveAction != null) Movement = _moveAction.ReadValue<Vector2>();
        if (_jumpAction != null)
        {
            JumpJustPressed = _jumpAction.WasPressedThisFrame();
            JumpBeingHeld = _jumpAction.IsPressed();
            JumpReleased = _jumpAction.WasReleasedThisFrame();
        }
    }
}
