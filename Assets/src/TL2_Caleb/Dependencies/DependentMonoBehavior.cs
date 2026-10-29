using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class DependentMonoBehaviour : MonoBehaviour
{
    private readonly HashSet<Type> _missingTypes = new HashSet<Type>();
    protected bool IsInitialized { get; private set; }

    protected virtual void Awake()
    {
        // Automatically discover every IDependent<T> interface this script implements
        foreach (var iface in GetType().GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDependent<>))
            {
                _missingTypes.Add(iface.GetGenericArguments()[0]);
            }
        }
    }

    protected virtual void OnEnable()
    {
        if (!IsInitialized)
        {
            this.enabled = false;
            return;
        }
        OnEnabledAfterInitialization();
    }

    protected virtual void OnDisable()
    {
        if (IsInitialized)
        {
            OnDisabledAfterInitialization();
        }
    }

    /// <summary>
    /// Called during Phase 1 to register that a dependency has been safely assigned.
    /// </summary>
    public void TrackFulfilledType(Type type)
    {
        _missingTypes.Remove(type);
    }

    /// <summary>
    /// Called during Phase 2 to evaluate if the component has everything it needs to run.
    /// </summary>
    public void ProcessActivationGate()
    {
        if (!IsInitialized && _missingTypes.Count == 0)
        {
            IsInitialized = true;
            this.enabled = true;
            OnSetupComplete();
        }
    }

    public void LogMissingDependenciesError()
    {
        if (!IsInitialized && _missingTypes.Count > 0)
        {
            List<string> names = new List<string>();
            foreach (var t in _missingTypes) names.Add(t.Name);
            Debug.LogError($"❌ [{gameObject.name}] [{GetType().Name}] is missing dependencies: {string.Join(", ", names)}", this);
        }
    }

    // Simple hooks child classes can safely override
    protected virtual void OnSetupComplete() { }
    protected virtual void OnEnabledAfterInitialization() { }
    protected virtual void OnDisabledAfterInitialization() { }
}