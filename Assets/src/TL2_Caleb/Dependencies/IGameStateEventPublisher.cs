using UnityEngine;
using System;

namespace TL2_Dependencies
{
    public interface IGameStateEventPublisher
    {
        event Action OnGameStarted;
        event Action OnMenuEntered;

        event Action<Transform> OnPlayerSpawned;
        
        void NotifyPlayerSpawned(Transform playerTransform);
    }   
}