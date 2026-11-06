# Unity Dependency Injection Architecture Chat Log

This log chronicles the progressive development of a decoupled, highly performant, and safe **Generic Dependency Injection (DI)** system for Unity. The architecture intentionally avoids custom global static singletons or service locators, prevents client scripts from modifying primary references, and ensures scripts remain completely inert until all required dependencies are securely injected.

---

## 🛠️ Phase 1: Identifying the Prefab Bug

### The Original Code Context
The goal was to create an accessor script (`GSMReference`) to wrap and inject an unmodifiable singleton component (`GameStateManager` under the namespace `EscapeThe90s`) without exposing the raw singleton pointer globally. 

The original attempt contained a critical bug in the fallback instantiation sequence:
```csharp
GameObject spawnedGSM = Instantiate(gsmPrefab);
gsmRef = gsmPrefab.GetComponent<GameStateManager>(); // ❌ BUG: Fetched from the asset layout, not the cloned instance
```

### The Architectural Fix
The cloned instance reference returned by `Instantiate()` must be captured directly. In addition, the exposed property should be restricted to a read-only lambda getter (`=>`) to ensure consumer scripts cannot overwrite or alter the master manager pointer.

---

## ⚙️ Phase 2: Choosing Contextual Injection over Service Locators

While migrating towards a decoupled state, we explicitly evaluated and rejected a **Static Service Locator**. A Service Locator acts as a static gatekeeper, meaning it functions architecturally as a singleton in disguise. It hides class dependencies and introduces global state manipulation vectors.

Instead, we designed a **Contextual Inversion of Control (IoC)** system using an automated Push/Inject framework.

### Key Performance Strategy: Single-Pass Caching
Calling `GetComponent` inside an execution frame loop (like `Update()`) causes high CPU overhead because Unity must cross into its underlying native C++ engine to sweep component types. 
To achieve maximum performance, this framework relies on **Caching**:
1. It executes a single sweep of the object hierarchy during `Awake()` or upon a dynamic object's instantiation frame.
2. It pushes raw pointer references into client variables exactly once.
3. Active runtime frames handle dependencies instantly with near-zero overhead.

---

## 🔒 Phase 3: The Multi-Dependency Initialization Barrier

To scale this to any dependency beyond the initial `GameStateManager`, we extracted a completely open generic interface (`IDependent<T>`). 

A critical functional requirement was introduced: **Dependent objects must remain completely non-functional until all required dependencies are safely received.**
* Scripts are unchecked/disabled by default in the Unity Inspector.
* Unity lifecycle loops (such as `Update()`) are entirely bypassed while a component is disabled.
* We rejected low-level bitwise tracking masks (`_state |= (1 << 1)`) because they sacrifice clean code readability for unnecessary initialization micro-optimizations.
* Instead, we implemented a robust diagnostic **`HashSet<string>` tracking pattern** that records the literal Type names of missing components, enabling descriptive runtime validation errors if an object fails to initialize.

---

## 📦 Phase 4: Production-Ready Implementation Files

Below is the complete, modular source configuration for the dependency injection framework.

### 1. The Core Interface Contract (`IDependent.cs`)
```csharp
public interface IDependent<T>
{
    void Inject(T dependency);
}
```

### 2. The Central Registry Container (`ServiceContainer.cs`)
```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ServiceContainer : MonoBehaviour
{
    [Header("Managed Dependency Prefabs")]
    [SerializeField] private GameObject gsmPrefab;
    [SerializeField] private GameObject audioManagerPrefab;

    // Secure mapping of Type -> Object Instance
    private readonly Dictionary<Type, object> _registry = new Dictionary<Type, object>();

    private void Awake()
    {
        RegisterAllDependencies();
        InjectAllSceneDependents();
        ValidateSceneDependents();
    }

    private void RegisterAllDependencies()
    {
        // Resolve and Register GameStateManager (Third-party Singleton Fallback)
        if (EscapeThe90s.GameStateManager.Instance != null)
        {
            Register(EscapeThe90s.GameStateManager.Instance);
        }
        else if (gsmPrefab != null)
        {
            GameObject spawnedGSM = Instantiate(gsmPrefab);
            Register(spawnedGSM.GetComponent<EscapeThe90s.GameStateManager>());
        }
        else
        {
            Debug.LogError("ServiceContainer: GameStateManager.Instance is null and no gsmPrefab assigned!", this);
        }

        // Resolve and Register AudioManager
        AudioManager existingAudio = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        if (existingAudio != null)
        {
            Register(existingAudio);
        }
        else if (audioManagerPrefab != null)
        {
            GameObject spawnedAudio = Instantiate(audioManagerPrefab);
            Register(spawnedAudio.GetComponent<AudioManager>());
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

    private void InjectAllSceneDependents()
    {
        var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mono in allMonoBehaviours)
        {
            InjectTarget(mono);
        }
    }

    public void InjectDynamicObject(GameObject spawnedObject)
    {
        var monos = spawnedObject.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mono in monos)
        {
            InjectTarget(mono);
        }
    }

    private void InjectTarget(MonoBehaviour mono)
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
                injectMethod?.Invoke(mono, new[] { dependencyInstance });
            }
        }
    }

    private void ValidateSceneDependents()
    {
        var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mono in allMonoBehaviours)
        {
            var method = mono.GetType().GetMethod("LogMissingDependenciesError");
            method?.Invoke(mono, null);
        }
    }
}
```

### 3. Example Dummy System Asset (`AudioManager.cs`)
```csharp
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public void PlaySound(string soundName)
    {
        Debug.Log($"🔊 Playing Audio Track: {soundName}");
    }
}
```

### 4. Concrete Consumer Client (`CombatHandler.cs`)
```csharp
using System.Collections.Generic;
using EscapeThe90s;
using UnityEngine;

// ⚠️ REMEMBER: Keep this component uncheck-disabled in the Unity Inspector by default!
// It will stay completely inert until all registered dependencies are safely injected.

public class CombatHandler : MonoBehaviour, 
    IDependent<GameStateManager>, 
    IDependent<AudioManager>
{
    private GameStateManager _gsm;
    private AudioManager _audioManager;

    // Track missing components explicitly by Type name
    private readonly HashSet<string> _missingDependencies = new HashSet<string>()
    {
        nameof(GameStateManager),
        nameof(AudioManager)
    };

    public void Inject(GameStateManager dependency)
    {
        _gsm = dependency;
        _missingDependencies.Remove(nameof(GameStateManager));
        VerifyInitializationGate();
    }

    public void Inject(AudioManager dependency)
    {
        _audioManager = dependency;
        _missingDependencies.Remove(nameof(AudioManager));
        VerifyInitializationGate();
    }

    private void VerifyInitializationGate()
    {
        if (_missingDependencies.Count == 0)
        {
            this.enabled = true; // Wake up component frame loops safely
            OnSetupComplete();
        }
    }

    private void OnSetupComplete()
    {
        Debug.Log($"[{gameObject.name}] CombatHandler: Initialization successful! All elements connected.", this);
        _audioManager.PlaySound("Battle_Music_Start");
    }

    public void LogMissingDependenciesError()
    {
        if (_missingDependencies.Count > 0)
        {
            string missingList = string.Join(", ", _missingDependencies);
            Debug.LogError($"❌ [{gameObject.name}] CombatHandler is non-functional! Missing dependencies: {missingList}", this);
        }
    }

    private void Update()
    {
        if (_gsm.currentGameState == "Paused") return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _audioManager.PlaySound("Sword_Slash_SFX");
        }
    }
}
```

### 5. Runtime Dynamic Factory Allocation (`EnemySpawner.cs`)
```csharp
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private ServiceContainer container;
    [SerializeField] private GameObject enemyPrefab;

    public void SpawnNewEnemy()
    {
        // 1. Physically clone the prefab layout into the active frame scene
        GameObject spawnedEnemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);

        // 2. Safely push required dependencies down the child hierarchy and activate them
        container.InjectDynamicObject(spawnedEnemy);
    }
}
```
