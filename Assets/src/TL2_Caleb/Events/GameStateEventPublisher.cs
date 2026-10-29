using UnityEngine;
using EscapeThe90s.GameStates;
using EscapeThe90s;
using TL2_Dependencies;

public class GameStateEventPublisher : DependentMonoBehaviour, IDependent<GameStateManager>, IGameStateEventPublisher
{
    public event System.Action OnGameStarted;
    public event System.Action OnMenuEntered;
    public event System.Action<Transform> OnPlayerSpawned; // 🚀 Implement event

    private GameStateManager _gsm;
    private readonly EventSubscriptionGroup _eventGroup = new EventSubscriptionGroup();

    public void Inject(GameStateManager dependency)
    {
        _gsm = dependency;
        _gsm.OnStateChanged += OnStateEnter;

        _eventGroup.Add(
            null,
            subscribe: h => { },
            unsubscribe: h => { if (_gsm != null) _gsm.OnStateChanged -= OnStateEnter; }
        );
    }

    // 🚀 Implement notice method
    public void NotifyPlayerSpawned(Transform playerTransform)
    {
        OnPlayerSpawned?.Invoke(playerTransform);
    }

    protected override void OnEnabledAfterInitialization()
    {
        if (_gsm != null)
        {
            _gsm.OnStateChanged -= OnStateEnter;
            _gsm.OnStateChanged += OnStateEnter;
        }

        if (_gsm != null && _gsm.CurrentState != null)
        {
            OnStateEnter(_gsm.CurrentState);
        }
    }

    protected override void OnDisabledAfterInitialization() => _eventGroup.UnsubscribeAll();

    private void OnStateEnter(GameState GS)
    {
        switch (GS)
        {
            case PlayingState: OnGameStarted?.Invoke(); break;
            case MenuState: OnMenuEntered?.Invoke(); break;
            case null: Debug.LogError("Game state is null!"); break;
            default: Debug.LogWarning($"Unknown State: {GS.GetType().Name}"); break;
        }
    }
}