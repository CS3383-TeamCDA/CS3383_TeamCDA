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

    private void Awake()
    {
        RegisterAllDependencies();
        
        // PHASE 1: Safely inject raw pointers while components are still asleep.
        InjectAllSceneDependents();
        
        // PHASE 2: Now that every script is 100% wired, process fulfillment gates in a batch sweep.
        WakeAllSceneDependents();
        
        ValidateSceneDependents();
    }

    /// <summary>
    /// Phase 1 Step: Scans the hierarchy and injects dependencies without waking up scripts.
    /// </summary>
    private void InjectAllSceneDependents()
    {
        var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);

        foreach (var mono in allMonoBehaviours)
        {
            InjectTargetOnly(mono); 
        }
    }

    /// <summary>
    /// Phase 2 Step: Sweeps components and instructs the base class to verify tracking counts.
    /// </summary>
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
        var monos = spawnedObject.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mono in monos)
        {
            InjectTargetOnly(mono);
        }
        
        // Wake them immediately since the scene setup sweep has already run
        foreach (var mono in monos)
        {
            if (mono is DependentMonoBehaviour dependent)
            {
                dependent.ProcessActivationGate();
            }
        }
    }

    /// <summary>
    /// Performs the reflection work to push dependencies without immediately kicking off lifecycle gates.
    /// </summary>
    private void InjectTargetOnly(MonoBehaviour mono)
    {
        if (mono == null) return;

        Type targetType = mono.GetType();

        foreach (var pair in _registry)
        {
            Type registeredType = pair.Key;
            object dependencyInstance = pair.Value;

            Type dependentInterface = typeof(IDependent<>).MakeGenericType(registeredType);

            if (dependentInterface.IsAssignableFrom(targetType))
            {
                MethodInfo injectMethod = targetType.GetMethod(nameof(IDependent<object>.Inject), new[] { registeredType });
                if (injectMethod != null)
                {
                    injectMethod.Invoke(mono, new[] { dependencyInstance });
                    
                    // Mark the type as ready inside the base class registry tracking stack
                    if (mono is DependentMonoBehaviour dependentBase)
                    {
                        dependentBase.TrackFulfilledType(registeredType);
                    }
                }
            }
        }
    }

    // Maintained for backward compatibility or direct manual calls
    private void InjectTarget(MonoBehaviour mono)
    {
        InjectTargetOnly(mono);
        if (mono is DependentMonoBehaviour dependent)
        {
            dependent.ProcessActivationGate();
        }
    }
}