#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using EscapeThe90s;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure;
using EscapeThe90s.Infrastructure.Unity;
using EscapeThe90s.PowerUps;
using LegacyUnityPublisher = EscapeThe90s.Dependencies.Unity.IUnityGameStateEventPublisher;
using UnityEngine;

namespace EscapeThe90s.Composition
{
    /// <summary>
    /// Game composition root for scene services. It creates/resolves game-owned providers,
    /// registers their contracts, then delegates injection and activation to DependencyScope.
    /// </summary>
    public sealed class ServiceContainer : MonoBehaviour
    {
        [Header("Managed Dependency Prefabs")]
        [SerializeField] GameObject gameStateManagerPrefab;
        [SerializeField] GameObject gameStateBridgePrefab;

        readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        DependencyScope _scope;

        void Awake()
        {
            GameStateManager gameStateManager = ResolveGameStateManager();
            EventBus eventBus = new EventBus();
            GameStateEventBridge bridge = ResolveBridge();

            if (gameStateManager != null)
            {
                // Only GameStateEventBridge needs this; remove it with the bridge once TL2 uses IGameState
                Register(gameStateManager);

                // Keep 
                Register<IGameState>(gameStateManager);
            }
            else Debug.LogError("ServiceContainer: GameStateManager could not be resolved or created.", this);

            // One scene-owned TL4 spawner is exposed to all contract consumers.
            foreach (EnemySpawner spawner in FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include))
            {
                if (spawner.gameObject.scene != gameObject.scene) continue;
                if (_services.ContainsKey(typeof(IEnemySpawner)))
                {
                    Debug.LogError("ServiceContainer: multiple scene EnemySpawners; use one shared provider.", this);
                    continue;
                }
                Register<IEnemySpawner>(spawner);
                if (spawner.GetComponent<EnemyGameStateBinding>() == null)
                    spawner.gameObject.AddComponent<EnemyGameStateBinding>();
            }

            // One scene-owned TL3 power-up manager is exposed to all contract consumers.
            foreach (PowerUpManager manager in FindObjectsByType<PowerUpManager>(FindObjectsInactive.Include))
            {
                if (manager.gameObject.scene != gameObject.scene) continue;
                if (_services.ContainsKey(typeof(IPowerUpSpawner)))
                {
                    Debug.LogError("ServiceContainer: multiple scene PowerUpManagers; use one shared provider.", this);
                    continue;
                }
                Register<IPowerUpSpawner>(manager);
            }

            // Resolve through the contract so composition does not depend on TL5's assembly.
            foreach (MonoBehaviour component in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (component.gameObject.scene != gameObject.scene ||
                    !(component is IMetaProgressionManager progression)) continue;
                if (_services.ContainsKey(typeof(IMetaProgressionManager)))
                {
                    Debug.LogError("ServiceContainer: multiple scene currency providers; use one shared provider.", this);
                    continue;
                }
                Register<IMetaProgressionManager>(progression);
            }
            if (_services.ContainsKey(typeof(IEnemySpawner)) &&
                _services.ContainsKey(typeof(IMetaProgressionManager)) &&
                GetComponent<EnemyCurrencyBinding>() == null)
                gameObject.AddComponent<EnemyCurrencyBinding>();

            Register<IEventBus>(eventBus);
            if (bridge != null)
            {
                Register<IGameStateEventPublisher>(bridge);
                Register<LegacyUnityPublisher>(bridge);
                Register<IGameplaySessionState>(bridge);
            }
            else Debug.LogError("ServiceContainer: GameStateEventBridge could not be resolved or created.", this);

            _scope = GetComponent<DependencyScope>();
            if (_scope == null) _scope = gameObject.AddComponent<DependencyScope>();
            foreach (KeyValuePair<Type, object> service in _services)
                _scope.Register(service.Key, service.Value);

            // Inject every dependency first, then activate gated components. This prevents
            // startup events from firing before all listeners have been wired.
            _scope.InjectSceneDependencies();
            _scope.ActivateSceneDependencies();
        }

        GameStateManager ResolveGameStateManager()
        {
            GameStateManager manager = GameStateManager.Instance;
            if (manager == null)
                manager = FindAnyObjectByType<GameStateManager>(FindObjectsInactive.Include);
            if (manager == null && gameStateManagerPrefab != null)
                manager = Instantiate(gameStateManagerPrefab).GetComponent<GameStateManager>();
            return manager;
        }

        GameStateEventBridge ResolveBridge()
        {
            foreach (MonoBehaviour component in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if (component is GameStateEventBridge bridge) return bridge;
            if (gameStateBridgePrefab == null) return null;
            return Instantiate(gameStateBridgePrefab).GetComponent<GameStateEventBridge>();
        }

        void Register<T>(T service) where T : class
        {
            Type contract = typeof(T);
            if (!contract.IsInstanceOfType(service))
                throw new ArgumentException($"Provider does not implement {contract.Name}.", nameof(service));
            _services[contract] = service;
        }

        /// <summary>Injects a runtime-created object before its dependency-aware scripts run.</summary>
        public void InjectDynamicObject(GameObject spawnedObject)
        {
            if (_scope == null)
            {
                Debug.LogError("ServiceContainer: DependencyScope has not been initialized.", this);
                return;
            }
            _scope.Inject(spawnedObject);
        }
    }
}
