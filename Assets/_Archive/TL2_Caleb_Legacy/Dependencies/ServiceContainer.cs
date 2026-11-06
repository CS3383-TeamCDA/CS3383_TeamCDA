using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ServiceContainer : MonoBehaviour
{
    [Header("Managed Dependency Prefabs")]
    [SerializeField] private GameObject gsmPrefab;
    [SerializeField] private GameObject gsepPrefab;

    private readonly Dictionary<Type, object> _registry = new Dictionary<Type, object>();
    private EscapeThe90s.Dependencies.Unity.DependencyScope _dependencyScope;

    private void Awake()
    {
        RegisterAllDependencies();
        _dependencyScope = GetComponent<EscapeThe90s.Dependencies.Unity.DependencyScope>();
        if (_dependencyScope == null) _dependencyScope = gameObject.AddComponent<EscapeThe90s.Dependencies.Unity.DependencyScope>();
        foreach (var dependency in _registry)
            _dependencyScope.Register(dependency.Key, dependency.Value);
        _dependencyScope.InjectSceneDependencies();
        WakeAllSceneDependents();
        _dependencyScope.ActivateSceneDependencies();
        ValidateSceneDependents();
    }

    /// <summary>Activates legacy components that still use the archived lifecycle gate.</summary>
    private void WakeAllSceneDependents()
    {
        var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);

        foreach (var mono in allMonoBehaviours)
        {
            if (mono is DependentMonoBehaviour dependent)
            {
                // Tells the base class to process its collected dependency list and turn on
                dependent.ProcessActivationGate(); 
            }
        }
    }

    private void ValidateSceneDependents()
    {
        var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);

        foreach (var mono in allMonoBehaviours)
        {
            if (mono == null) continue;
            if (mono is EscapeThe90s.Dependencies.Unity.DependencyBehaviour) continue;
            
            var method = mono.GetType().GetMethod("LogMissingDependenciesError");
            if (method != null)
            {
                method.Invoke(mono, null);
            }
        }
    }

    private void RegisterAllDependencies()
    {
        EscapeThe90s.GameStateManager masterGSM = null;

        if (EscapeThe90s.GameStateManager.Instance != null)
        {
            masterGSM = EscapeThe90s.GameStateManager.Instance;
        }
        else
        {
            masterGSM = FindAnyObjectByType<EscapeThe90s.GameStateManager>(FindObjectsInactive.Include);
        }

        if (masterGSM == null && gsmPrefab != null)
        {
            GameObject spawnedGSM = Instantiate(gsmPrefab);
            masterGSM = spawnedGSM.GetComponent<EscapeThe90s.GameStateManager>();
        }

        if (masterGSM != null)
        {
            Register(masterGSM);
        }
        else
        {
            Debug.LogError("ServiceContainer: GameStateManager could not be resolved!", this);
        }

        // 2. Resolve and Register GameStateEventPublisher strictly via its Interface
        TL2_Dependencies.IGameStateEventPublisher publisherInstance = null;
        var sceneMonos = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        
        foreach (var mono in sceneMonos)
        {
            if (mono is TL2_Dependencies.IGameStateEventPublisher match)
            {
                publisherInstance = match;
                break;
            }
        }

        if (publisherInstance == null && gsepPrefab != null)
        {
            GameObject spawnedGSEP = Instantiate(gsepPrefab);
            publisherInstance = spawnedGSEP.GetComponent<TL2_Dependencies.IGameStateEventPublisher>();
        }

        if (publisherInstance != null)
        {
            Register<TL2_Dependencies.IGameStateEventPublisher>(publisherInstance);
            Register<EscapeThe90s.Contracts.IGameStateEventPublisher>(publisherInstance);
        }
        else
        {
            Debug.LogError("ServiceContainer: GSEP instance could not be found or spawned via IGameStateEventPublisher interface!", this);
        }
    }

    public void Register<T>(T instance)
    {
        Type type = typeof(T);
        if (!_registry.ContainsKey(type))
        {
            _registry.Add(type, instance);
        }
        else
        {
            Debug.LogWarning($"ServiceContainer: Type {type.Name} is already registered.", this);
        }
    }

    /// <summary>
    /// Explicitly inject and wake a mid-game dynamically spawned object in one pass.
    /// </summary>
    public void InjectDynamicObject(GameObject spawnedObject)
    {
        if (_dependencyScope == null)
        {
            Debug.LogError("ServiceContainer: DependencyScope has not been initialized.", this);
            return;
        }
        _dependencyScope.Inject(spawnedObject);
    }

}
