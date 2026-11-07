using UnityEngine;

namespace EscapeThe90s.Infrastructure.Unity
{
    /// <summary>Unity-only player spawn message; Contracts remains engine independent.</summary>
    public readonly struct UnityPlayerSpawnedEvent
    {
        public Transform Player { get; }
        public UnityPlayerSpawnedEvent(Transform player) => Player = player;
    }
}
