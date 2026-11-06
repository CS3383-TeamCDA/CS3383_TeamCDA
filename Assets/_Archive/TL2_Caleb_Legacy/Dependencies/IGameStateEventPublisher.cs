using UnityEngine;
using System;

namespace TL2_Dependencies
{
    public interface IGameStateEventPublisher : EscapeThe90s.Contracts.IGameStateEventPublisher
    {
        event Action<Transform> OnPlayerSpawned;
        Transform CurrentPlayer { get; }

        void NotifyPlayerSpawned(Transform playerTransform);
    }   
}
