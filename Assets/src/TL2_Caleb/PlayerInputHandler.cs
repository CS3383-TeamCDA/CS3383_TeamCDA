using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 Movement {get; private set;}
    public bool JumpJustPressed {get; private set;}
    public bool JumpBeingHeld {get; private set;}
    public bool JumpReleased {get; private set;}

    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private GameStateEventPublisher GameStateEvents;

    private InputAction _moveAction;
    private InputAction _jumpAction;

    [SerializeField] private GameObject firstMenuButton;
    [SerializeField] private GameObject menuUI;

    private void Awake()
    {
        SetupInputActions();
    }

    private void Update()
    {
        if (_playerInput.currentActionMap != null && _playerInput.currentActionMap.name == "InGame")
        {
            UpdateInputs();
        }
    }

    private void OnEnable()
    {
        GameStateEvents.OnGameStarted += EnterGame;
        GameStateEvents.OnEnteredMenu += EnterUI;
    }

    private void OnDisable()
    {
        GameStateEvents.OnGameStarted -= EnterGame;
        GameStateEvents.OnEnteredMenu -= EnterUI;
    }

    private void SetupInputActions()
    {
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];
    }

    private void UpdateInputs()
    {
        InputAction moveAction = _playerInput.actions.FindAction("Move");
        InputAction jumpAction = _playerInput.actions.FindAction("Jump");

        if (moveAction != null)
        {
            Movement = moveAction.ReadValue<Vector2>(); // Fixed generic type parameter
        }

        if (jumpAction != null)
        {
            JumpJustPressed = jumpAction.WasPressedThisFrame();
            JumpBeingHeld = jumpAction.IsPressed();
            JumpReleased = jumpAction.WasReleasedThisFrame();
        }
    }

    private void EnterUI()
    {
        ClearInputs();

        //_playerInput.actions.FindActionMap("InGame").Disable();
        //_playerInput.actions.FindActionMap("UI").Enable();

        _playerInput.SwitchCurrentActionMap("UI");

        if (firstMenuButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstMenuButton);
        }
    }

    private void EnterGame()
    {
        ClearInputs();

        // 1. Disable the UI Action Map so it stops taking input
        //_playerInput.actions.FindActionMap("UI").Disable();

        // 2. Enable the Gameplay/Player Action Map
        //_playerInput.actions.FindActionMap("InGame").Enable();

        // 3. Switch the PlayerInput component's current map context
        _playerInput.SwitchCurrentActionMap("InGame");

        // 4. Break the UI focus so the button doesn't stay "stuck" or highlighted
        EventSystem.current.SetSelectedGameObject(null);

        // Hide menu
        if (menuUI != null)
        {
            menuUI.SetActive(false);
        }
    }

    private void ClearInputs()
    {
        Movement = Vector2.zero;
        JumpJustPressed = false;
        JumpBeingHeld = false;
        JumpReleased = false;
    }
}
