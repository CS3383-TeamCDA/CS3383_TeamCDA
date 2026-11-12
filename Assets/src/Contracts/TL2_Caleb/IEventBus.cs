using System;

namespace EscapeThe90s.Contracts
{
    /// <summary>Typed in-process messaging contract shared by gameplay features.</summary>
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent message);
        IDisposable Subscribe<TEvent>(Action<TEvent> handler, bool replayLatest = false);
    }
}
