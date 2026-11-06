using System;
using System.Collections.Generic;

public class EventSubscriptionGroup
{
    private readonly List<SafeEventSubscription> _trackedSubscriptions = new List<SafeEventSubscription>();

    /// <summary>
    /// Registers a parameterless action event into this managed group.
    /// </summary>
    public void Add(Action handler, Action<Action> subscribe, Action<Action> unsubscribe)
    {
        var sub = new SafeEventSubscription(handler);
        sub.Bind(subscribe, unsubscribe);
        _trackedSubscriptions.Add(sub);
    }

    /// <summary>
    /// Registers a parameterized action event into this managed group.
    /// </summary>
    public void Add<T>(Action<T> handler, Action<Action<T>> subscribe, Action<Action<T>> unsubscribe)
    {
        var sub = new SafeEventSubscription<T>(handler);
        sub.Bind(subscribe, unsubscribe);
        // Note: Requires making a common non-generic interface or object cast array 
        // if mixing <T> variants inside the same list, or keeping distinct storage groups.
    }

    /// <summary>
    /// Connects and registers all underlying event handlers to their event streams simultaneously.
    /// </summary>
    public void SubscribeAll()
    {
        foreach (var sub in _trackedSubscriptions) 
        {
            sub.Subscribe();
        }
    }

    /// <summary>
    /// Disconnects and purges all underlying event handlers from their event streams simultaneously.
    /// </summary>
    public void UnsubscribeAll()
    {
        foreach (var sub in _trackedSubscriptions) 
        {
            sub.Unsubscribe();
        }
    }
}