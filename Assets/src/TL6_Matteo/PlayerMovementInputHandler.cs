using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementInputHandler : MonoBehaviour, IPlayerMovementInput
{
    [SerializeField] private PlayerInput _playerInput;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;

    public Vector2 Movement { get; private set; }
    public bool JumpJustPressed { get; private set; }
    public bool JumpBeingHeld { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool DashJustPressed { get; private set; }



    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        _moveAction = _playerInput.actions.FindAction("Move");
        _jumpAction = _playerInput.actions.FindAction("Jump");
        _dashAction = _playerInput.actions.FindAction("Dash");
    }

    private void Update()
    {
        UpdateInputs();
    }

    private void UpdateInputs()
    {
        if (_moveAction != null)
            Movement = _moveAction.ReadValue<Vector2>();

        if (_jumpAction != null)
        {
            JumpJustPressed = _jumpAction.WasPressedThisFrame();
            JumpBeingHeld = _jumpAction.IsPressed();
            JumpReleased = _jumpAction.WasReleasedThisFrame();
        }

        if (_dashAction != null)
        {
            DashJustPressed = _dashAction.WasPressedThisFrame();
        }
    }
}
