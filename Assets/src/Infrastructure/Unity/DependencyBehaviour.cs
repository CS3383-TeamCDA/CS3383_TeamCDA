using System;
using System.Collections.Generic;
using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.Infrastructure.Unity
{
    /// <summary>
    /// Optional lifecycle gate for scene components that require injected services.
    /// Inject all required services, then call ProcessActivationGate once composition is complete.
    /// </summary>
    public abstract class DependencyBehaviour : MonoBehaviour
    {
        readonly HashSet<Type> _missingDependencies = new HashSet<Type>();
        bool _requirementsDiscovered;

        protected bool IsInitialized { get; private set; }

        protected virtual void Awake() => DiscoverRequirements();

        protected virtual void OnEnable()
        {
            DiscoverRequirements();
            if (!IsInitialized)
            {
                enabled = false;
                return;
            }
            OnEnabledAfterInitialization();
        }

        protected virtual void OnDisable()
        {
            if (IsInitialized) OnDisabledAfterInitialization();
        }

        public void TrackFulfilledType(Type dependencyType)
        {
            DiscoverRequirements();
            _missingDependencies.Remove(dependencyType);
        }

        public void ProcessActivationGate()
        {
            DiscoverRequirements();
            if (IsInitialized || _missingDependencies.Count != 0) return;

            IsInitialized = true;
            OnSetupComplete();
            if (!enabled) enabled = true;
        }

        public IReadOnlyCollection<Type> GetMissingDependencies()
        {
            DiscoverRequirements();
            return _missingDependencies;
        }

        public void LogMissingDependenciesError()
        {
            if (!IsInitialized && _missingDependencies.Count > 0)
            {
                var names = new List<string>();
                foreach (Type dependency in _missingDependencies) names.Add(dependency.Name);
                Debug.LogError($"[{gameObject.name}] {GetType().Name} is missing dependencies: {string.Join(", ", names)}", this);
            }
        }

        void DiscoverRequirements()
        {
            if (_requirementsDiscovered) return;
            _requirementsDiscovered = true;
            foreach (Type implemented in GetType().GetInterfaces())
            {
                if (implemented.IsGenericType &&
                    implemented.GetGenericTypeDefinition() == typeof(IDependent<>))
                    _missingDependencies.Add(implemented.GetGenericArguments()[0]);
            }
        }

        protected virtual void OnSetupComplete() { }
        protected virtual void OnEnabledAfterInitialization() { }
        protected virtual void OnDisabledAfterInitialization() { }
    }
}
