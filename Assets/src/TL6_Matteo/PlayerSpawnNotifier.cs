using UnityEngine;
using TL2_Dependencies;
using EscapeThe90s.Dependencies.Unity;

public class PlayerSpawnNotifier : DependencyBehaviour,
    EscapeThe90s.Contracts.IDependent<IGameStateEventPublisher>
{
    private IGameStateEventPublisher _publisher;

    
    public void Inject(IGameStateEventPublisher publisher)
    {
        if (publisher == null)
        {
            Debug.LogError("PlayerSpawnNotifier: Missing event publisher.");
            return;
        }
        _publisher = publisher;
    }

    protected override void OnSetupComplete() => _publisher?.NotifyPlayerSpawned(transform);
}
