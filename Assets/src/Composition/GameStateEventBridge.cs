#pragma warning disable CS0618
using EscapeThe90s.Contracts;
using EscapeThe90s.GameStates;
using EscapeThe90s.Infrastructure.Unity;
using LegacyUnityPublisher = EscapeThe90s.Dependencies.Unity.IUnityGameStateEventPublisher;
using UnityEngine;

namespace EscapeThe90s.Composition
{
    /// <summary>Composition adapter translating game-owned state transitions into shared messages.</summary>
    public sealed class GameStateEventBridge : EscapeThe90s.Infrastructure.Unity.DependencyBehaviour,
        IDependent<GameStateManager>, IDependent<IEventBus>, LegacyUnityPublisher, IGameplaySessionState
    {
        GameStateManager _manager;
        IEventBus _events;
        bool _listening;
        System.IDisposable _spawnSubscription;

        public event System.Action OnGameStarted;
        public event System.Action OnMenuEntered;
        public event System.Action OnPlayerDied;
        public event System.Action<UnityEngine.Transform> OnPlayerSpawned;
        public UnityEngine.Transform CurrentPlayer { get; private set; }
        public bool IsGameplayStarted => _manager == null || _manager.CurrentState is PlayingState;

        public void Inject(GameStateManager manager) { _manager = manager; if (IsInitialized) StartListening(); }
        public void Inject(IEventBus events) { _events = events; if (IsInitialized) StartListening(); }

        public void NotifyPlayerSpawned(UnityEngine.Transform player)
        {
            if (player == null) return;
            CurrentPlayer = player;
            OnPlayerSpawned?.Invoke(player);
        }

        public void NotifyPlayerDied()
        {
            CurrentPlayer = null;
            OnPlayerDied?.Invoke();
            _events?.Publish(new PlayerDiedEvent());
        }

        protected override void OnSetupComplete() => StartListening();
        protected override void OnEnabledAfterInitialization() => StartListening();
        protected override void OnDisabledAfterInitialization() => StopListening();

        void StartListening()
        {
            if (!_listening && _manager != null)
            {
                _manager.OnStateChanged += HandleStateChanged;
                _listening = true;
                if (_manager.CurrentState != null) HandleStateChanged(_manager.CurrentState);
            }
            if (_spawnSubscription == null && _events != null)
                _spawnSubscription = _events.Subscribe<UnityPlayerSpawnedEvent>(message => NotifyPlayerSpawned(message.Player), true);
        }

        void StopListening()
        {
            if (_listening && _manager != null) _manager.OnStateChanged -= HandleStateChanged;
            _listening = false;
            _spawnSubscription?.Dispose();
            _spawnSubscription = null;
        }

        void HandleStateChanged(GameState state)
        {
            if (state is PlayingState)
            {
                if (_manager.PreviousState is PauseState) _events?.Publish(new GameResumedEvent());
                else _events?.Publish(new GameStartedEvent());
                OnGameStarted?.Invoke();
            }
            else if (state is MenuState) { _events?.Publish(new MenuEnteredEvent()); OnMenuEntered?.Invoke(); }
            else if (state is PauseState) _events?.Publish(new GamePausedEvent());
            else if (state is GameOverState) _events?.Publish(new GameOverEvent());
        }
    }
}
