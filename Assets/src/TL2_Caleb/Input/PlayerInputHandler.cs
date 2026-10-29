using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TL2_Dependencies; // 🚀 ONLY import your local dependencies contract!

public class PlayerInputHandler : DependentMonoBehaviour, IDependent<IGameStateEventPublisher>
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private GameObject firstMenuButton;
    [SerializeField] private GameObject menuUI;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    
    private readonly EventSubscriptionGroup _eventGroup = new EventSubscriptionGroup();

    public Vector2 Movement { get; private set; }
    public bool JumpJustPressed { get; private set; }
    public bool JumpBeingHeld { get; private set; }
    public bool JumpReleased { get; private set; }

    protected override void Awake()
    {
        base.Awake(); 
        CacheInputActions();
    }

    public void Inject(IGameStateEventPublisher gsep)
    {
        CacheInputActions();
        
        // 🚀 We bind directly to the clean interface proxy events.
        _eventGroup.Add(EnterGame, h => gsep.OnGameStarted += h, h => gsep.OnGameStarted -= h);
        _eventGroup.Add(EnterUI, h => gsep.OnMenuEntered += h, h => gsep.OnMenuEntered -= h);
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

    protected override void OnEnabledAfterInitialization()
    {
        // 🚀 When the player turns on, we activate our subscription group.
        // Because GameStateEventPublisher immediately checks its current state 
        // upon waking up, it will instantly pass the active frame context to us,
        // triggering EnterGame() safely the exact moment the object is active!
        _eventGroup.SubscribeAll();
    }

    protected override void OnDisabledAfterInitialization() => _eventGroup.UnsubscribeAll();

    private void Update()
    {
        if (_playerInput != null && _playerInput.currentActionMap != null && _playerInput.currentActionMap.name == "InGame")
        {
            UpdateInputs();
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

    private void EnterUI()
    {
        ClearInputs();
        if (_playerInput != null && _playerInput.gameObject.activeInHierarchy) 
        {
            _playerInput.SwitchCurrentActionMap("UI");
        }
        if (firstMenuButton != null) EventSystem.current.SetSelectedGameObject(firstMenuButton);
    }

    private void EnterGame()
    {
        ClearInputs();
        if (_playerInput != null && _playerInput.gameObject.activeInHierarchy) 
        {
            _playerInput.SwitchCurrentActionMap("InGame");
        }
        EventSystem.current.SetSelectedGameObject(null);
        if (menuUI != null) menuUI.SetActive(false);
    }

    private void ClearInputs()
    {
        Movement = Vector2.zero;
        JumpJustPressed = JumpBeingHeld = JumpReleased = false;
    }
}