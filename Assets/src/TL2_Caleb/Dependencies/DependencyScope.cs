using System;
using System.Collections.Generic;
using System.Reflection;
using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.Dependencies.Unity
{
    /// <summary>
    /// Small scene composition helper. Register contract/provider pairs explicitly, inject a
    /// scene once, and call Inject on objects created later. It does not construct game services.
    /// </summary>
    public sealed class DependencyScope : MonoBehaviour
    {
        readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        bool _hasInjectedScene;

        public void Register<TContract>(TContract service) where TContract : class
        {
            Register(typeof(TContract), service);
        }

        public void Register(Type contract, object service)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (!contract.IsInstanceOfType(service))
                throw new ArgumentException($"{service.GetType().Name} does not implement {contract.Name}.", nameof(service));
            _services[contract] = service;
            if (_hasInjectedScene)
            {
                MonoBehaviour[] components = FindSceneComponents();
                InjectServiceIntoComponents(components, contract, service);
                ActivateComponents(components);
                ReportMissingDependencies(components);
            }
        }

        public void InjectScene()
        {
            InjectSceneDependencies();
            ActivateSceneDependencies();
        }

        public void InjectSceneDependencies()
        {
            if (_hasInjectedScene) return;
            MonoBehaviour[] components = FindSceneComponents();
            InjectComponents(components);
            _hasInjectedScene = true;
        }

        public void ActivateSceneDependencies()
        {
            MonoBehaviour[] components = FindSceneComponents();
            ActivateComponents(components);
            ReportMissingDependencies(components);
        }

        public void Inject(GameObject instance)
        {
            if (instance == null) return;
            MonoBehaviour[] components = instance.GetComponentsInChildren<MonoBehaviour>(true);
            InjectComponents(components);
            ActivateComponents(components);
            ReportMissingDependencies(components);
        }

        void InjectComponents(MonoBehaviour[] components)
        {
            foreach (KeyValuePair<Type, object> service in _services)
                InjectServiceIntoComponents(components, service.Key, service.Value);
        }

        static void InjectServiceIntoComponents(MonoBehaviour[] components, Type contract, object service)
        {
            Type dependentType = typeof(IDependent<>).MakeGenericType(contract);
            foreach (MonoBehaviour component in components)
            {
                if (component == null) continue;
                if (!dependentType.IsAssignableFrom(component.GetType())) continue;
                MethodInfo inject = dependentType.GetMethod(nameof(IDependent<object>.Inject));
                inject?.Invoke(component, new[] { service });
                if (component is DependencyBehaviour gated) gated.TrackFulfilledType(contract);
            }
        }

        static void ActivateComponents(MonoBehaviour[] components)
        {
            foreach (MonoBehaviour component in components)
                if (component is DependencyBehaviour gated) gated.ProcessActivationGate();
        }

        static void ReportMissingDependencies(MonoBehaviour[] components)
        {
            foreach (MonoBehaviour component in components)
            {
                if (!(component is DependencyBehaviour gated)) continue;
                foreach (Type missing in gated.GetMissingDependencies())
                    Debug.LogError($"[{component.name}] {component.GetType().Name} is missing {missing.Name}.", component);
            }
        }

        MonoBehaviour[] FindSceneComponents()
        {
            var found = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            var sceneComponents = new List<MonoBehaviour>(found.Length);
            foreach (MonoBehaviour component in found)
                if (component != null && component.gameObject.scene == gameObject.scene)
                    sceneComponents.Add(component);
            return sceneComponents.ToArray();
        }
    }
}
