using System;

// --- for parameterized events like Action<GameState> ---
public class SafeEventSubscription<T>
{
    private readonly Action<T> _handler;
    private Action<Action<T>> _subscribe;
    private Action<Action<T>> _unsubscribe;
    private bool _isSubscribed;

    public SafeEventSubscription(Action<T> handler) => _handler = handler;

    public void Bind(Action<Action<T>> subscribeAction, Action<Action<T>> unsubscribeAction)
    {
        _subscribe = subscribeAction;
        _unsubscribe = unsubscribeAction;
    }

    public void Subscribe()
    {
        if (_isSubscribed || _subscribe == null) return;
        _subscribe(_handler);
        _isSubscribed = true;
    }

    public void Unsubscribe()
    {
        if (!_isSubscribed || _unsubscribe == null) return;
        _unsubscribe(_handler);
        _isSubscribed = false;
    }
}

// --- for parameterless events like Action ---
public class SafeEventSubscription
{
    private readonly Action _handler;
    private Action<Action> _subscribe;
    private Action<Action> _unsubscribe;
    private bool _isSubscribed;

    public SafeEventSubscription(Action handler) => _handler = handler;

    public void Bind(Action<Action> subscribeAction, Action<Action> unsubscribeAction)
    {
        _subscribe = subscribeAction;
        _unsubscribe = unsubscribeAction;
    }

    public void Subscribe()
    {
        if (_isSubscribed || _subscribe == null) return;
        _subscribe(_handler);
        _isSubscribed = true;
    }

    public void Unsubscribe()
    {
        if (!_isSubscribed || _unsubscribe == null) return;
        _unsubscribe(_handler);
        _isSubscribed = false;
    }
}