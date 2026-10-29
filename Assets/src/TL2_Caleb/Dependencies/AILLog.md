# Unity Dependency Injection - AI Chat Log

**Topic:** Decoupling Unity Singletons and Implementing a Generic Dependency Container
**Date:** October 28, 2026
**Participants:** Developer & AI Assistant

---

## 📋 Overview
This log details the structural evolution of a Unity dependency handling system. The goal was to remove reliance on a third-party `GameStateManager` (GSM) global singleton, secure variables from being modified by external scripts, handle both scene-hierarchy and runtime-spawned objects, and prevent client scripts from running until all dependencies are fulfilled.

---

## 🪵 Chat History

### 💬 Entry 1: The Initial Problem
> **User:** get component
>
> **AI:** Explained `GetComponent<T>()` as a core built-in function in Unity used to retrieve a reference to a component on a GameObject. Highlighted best practices such as avoiding calls inside `Update()` and caching references in `Start()` or `Awake()`.

### 💬 Entry 2: Exposing a GSM Instance
> **User:** A script i create in unityt (monbehavior) has a variable that is an instance of a GSM. How can I recieve, in another game object, either that variable from an objhect with that script, or a gameobject that has a GSM script component
>
> **AI:** Provided three solutions:
> 1. *Singleton Pattern* (Global instance access).
> 2. *Direct Inspector Assignment* (Drag-and-drop reference).
> 3. *Finding via Code* (Using `Object.FindFirstObjectByType<T>()` at startup).

### 💬 Entry 3: Custom Accessor Logic & Fallback Fix
> **User:** It is already a singleton, i am trying to make an accessor so i can inject it instead of relying on the singleton in my own scripts, since i didn't creeate and cannot modify tyhe singleton [Shared code snippet using `Instantiate(gsmPrefab)`]
>
> **AI:** Identified a critical bug in the fallback logic: `GetComponent` was being called directly on the prefab asset instead of the newly spawned clone in the scene. Provided the corrected script capturing the clone and exposing it via a public read-only property to allow basic manual injection.

### 💬 Entry 4: Moving Beyond Service Locators
> **User:** They should not be able to set the GSM. And service locatorr would be a singleton wouldn't it ....
>
> **AI:** Validated that a Service Locator acts exactly like a singleton in disguise. Shifted the architecture to *Contextual Inversion of Control (IoC)* using a secure, push-based interface approach (`IGSMDependent`) to automatically find and inject dependencies without global state singletons.

### 💬 Entry 5: Making Components Inert Until Ready
> **User:** Right now they all exist in heirarchy. eventually they may be instantiated dynamically. Another thing is that we need to ensure they dont function intul they get gsm
>
> **AI:** Introduced the paradigm of unchecking / disabling dependent components by default in the Inspector. Scripts remain dead (ignoring `Update()`) until their injection interface method is called, which handles caching and flips `this.enabled = true`. Also provided a method to safely hook up objects instantiated dynamically mid-game.

### 💬 Entry 6: Performance & Caching Realities
> **User:** isn't get component expensive
>
> **AI:** Clarified that `GetComponent` or hierarchy sweeps are moderate, one-time performance operations when kept strictly inside `Awake()` or `Start()`. Because our injection pattern caches raw pointer references at birth, using the variables in `Update()` costs near zero performance.

### 💬 Entry 7: Generalizing for Any Dependency
> **User:** Sure. And since tthis cann apply to any dependency, why don't we make it not rely on gsm explicitly. we make a concrete for gsm, and specifically gsm required (wherre the object does not start until gsm is injected)
>
> **AI:** Abstracted the pattern completely using C# Generics (`IDependent<T>`). Built a `ServiceContainer` acting as a lightweight registry that links any type map to consumer components on load.

### 💬 Entry 8: Multi-Dependency Scaling
> **User:** Example of an object with multiple dependencies
>
> **AI:** Showed how a single script can implement multiple generic interfaces (e.g., `IDependent<GameStateManager>, IDependent<AudioManager>`). Implemented a safety gate ensuring `this.enabled = true` is only flipped once every required component has arrived.

---

## 🛠️ Complete Production Architecture Layout

Below is the complete set of structural code files finalized during the discussion.

### 1. The Interface (`IDependent.cs`)
```csharp
public interface IDependent<T>
{
    void Inject(T dependency);
}
```

### 2. The Registry (`ServiceContainer.cs`)
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

    private readonly Dictionary<Type, object> _registry = new Dictionary<Type, object>();

    private void Awake()
    {
        RegisterAllDependencies();
        InjectAllSceneDependents();
    }

    private void RegisterAllDependencies()
    {
        if (EscapeThe90s.GameStateManager.Instance != null)
        {
            Register(EscapeThe90s.GameStateManager.Instance);
        }
        else if (gsmPrefab != null)
        {
            GameObject spawnedGSM = Instantiate(gsmPrefab);
            Register(spawnedGSM.GetComponent<EscapeThe90s.GameStateManager>());
        }

        AudioManager existingAudio = Object.FindFirstObjectByType<AudioManager>();
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
        if (!_registry.ContainsKey(type)) _registry.Add(type, instance);
    }

    private void InjectAllSceneDependents()
    {
        var allMonoBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mono in allMonoBehaviours) InjectTarget(mono);
    }

    public void InjectDynamicObject(GameObject spawnedObject)
    {
        var monos = spawnedObject.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mono in monos) InjectTarget(mono);
    }

    private void InjectTarget(MonoBehaviour mono)
    {
        if (mono == null) return;
        Type targetType = mono.GetType();

        foreach (var pair in _registry)
        {
            Type dependentInterface = typeof(IDependent<>).MakeGenericType(pair.Key);
            if (dependentInterface.IsAssignableFrom(targetType))
            {
                MethodInfo injectMethod = targetType.GetMethod(nameof(IDependent<object>.Inject), new[] { pair.Key });
                injectMethod?.Invoke(mono, new[] { pair.Value });
            }
        }
    }
}
```

### 3. Multi-Dependent Consumer Sample (`CombatHandler.cs`)
```csharp
using EscapeThe90s;
using UnityEngine;

// ⚠️ Note: Keep unchecked/disabled inside the Unity Inspector window by default!
public class CombatHandler : MonoBehaviour, 
    IDependent<GameStateManager>, 
    IDependent<AudioManager>
{
    private GameStateManager _gsm;
    private AudioManager _audioManager;

    private int _currentDependenciesState = 0;
    private const int TargetDependenciesMet = (1 << 0) | (1 << 1); 

    public void Inject(GameStateManager dependency)
    {
        _gsm = dependency;
        _currentDependenciesState |= (1 << 0);
        VerifyInitializationGate();
    }

    public void Inject(AudioManager dependency)
    {
        _audioManager = dependency;
        _currentDependenciesState |= (1 << 1);
        VerifyInitializationGate();
    }

    private void VerifyInitializationGate()
    {
        if (_currentDependenciesState == TargetDependenciesMet)
        {
            this.enabled = true; 
            Debug.Log("CombatHandler: All structural components linked cleanly!");
        }
    }

    private void Update()
    {
        if (_gsm.currentGameState == "Paused") return;
        if (Input.GetKeyDown(KeyCode.Space)) _audioManager.PlaySound("Sword_Slash_SFX");
    }
}
```

---
*End of Chat Log File.*