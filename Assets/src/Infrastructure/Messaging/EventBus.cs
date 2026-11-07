using System;
using System.Collections.Generic;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.Infrastructure
{
    /// <summary>Typed in-process event bus with optional last-message replay per event type.</summary>
    public sealed class EventBus : IEventBus
    {
        readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();
        readonly Dictionary<Type, object> _latest = new Dictionary<Type, object>();

        public void Publish<TEvent>(TEvent message)
        {
            Type type = typeof(TEvent);
            _latest[type] = message;
            if (_handlers.TryGetValue(type, out Delegate handlers))
                ((Action<TEvent>)handlers).Invoke(message);
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler, bool replayLatest = false)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Type type = typeof(TEvent);
            _handlers.TryGetValue(type, out Delegate current);
            _handlers[type] = Delegate.Combine(current, handler);
            if (replayLatest && _latest.TryGetValue(type, out object latest))
                handler((TEvent)latest);
            return new Subscription(() => Remove(type, handler));
        }

        void Remove(Type type, Delegate handler)
        {
            if (!_handlers.TryGetValue(type, out Delegate current)) return;
            Delegate remaining = Delegate.Remove(current, handler);
            if (remaining == null) _handlers.Remove(type);
            else _handlers[type] = remaining;
        }

        sealed class Subscription : IDisposable
        {
            Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose()
            {
                Action dispose = _dispose;
                _dispose = null;
                dispose?.Invoke();
            }
        }
    }
}
