using UnityEngine;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;

public class PlayerSpawnNotifier : DependencyBehaviour, IDependent<IEventBus>
{
    private IEventBus _events;

    
    public void Inject(IEventBus events)
    {
        if (events == null)
        {
            Debug.LogError("PlayerSpawnNotifier: Missing event publisher.");
            return;
        }
        _events = events;
    }

    protected override void OnSetupComplete() => _events?.Publish(new UnityPlayerSpawnedEvent(transform));
}
