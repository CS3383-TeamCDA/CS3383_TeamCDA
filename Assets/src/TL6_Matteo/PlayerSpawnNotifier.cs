using UnityEngine;
using TL2_Dependencies;

public class PlayerSpawnNotifier : DependentMonoBehaviour,
    IDependent<IGameStateEventPublisher>
{
    
    public void Inject(IGameStateEventPublisher publisher)
    {
        if (publisher == null)
        {
            Debug.LogError("PlayerSpawnNotifier: Missing event publisher.");
            return;
        }

        publisher.NotifyPlayerSpawned(transform);
    }
}