#pragma warning disable CS0618
using System;
using UnityEngine;

namespace EscapeThe90s.Dependencies.Unity
{
    /// <summary>Compatibility aliases for existing feature assemblies; use Infrastructure.Unity for new code.</summary>
    [Obsolete("Use EscapeThe90s.Infrastructure.Unity.DependencyBehaviour.")]
    public abstract class DependencyBehaviour : EscapeThe90s.Infrastructure.Unity.DependencyBehaviour { }

    [Obsolete("Use EscapeThe90s.Infrastructure.Unity.DependencyScope.")]
    public class DependencyScope : EscapeThe90s.Infrastructure.Unity.DependencyScope { }

    /// <summary>Compatibility publisher contract; new consumers should use IEventBus.</summary>
    [Obsolete("Use EscapeThe90s.Contracts.IEventBus with typed messages.")]
    public interface IUnityGameStateEventPublisher : EscapeThe90s.Contracts.IGameStateEventPublisher
    {
        event Action<Transform> OnPlayerSpawned;
        Transform CurrentPlayer { get; }
        void NotifyPlayerSpawned(Transform playerTransform);
    }
}
