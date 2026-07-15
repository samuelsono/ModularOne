using CarTrack.Core.Messaging;

namespace CarTrack.Infrastructure.Messaging;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;

    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : class, IIntegrationEvent;
}

/// <summary>
/// In-process, synchronous fan-out event bus. Suitable for the modular monolith;
/// replace with outbox later if needed.
/// </summary>
public sealed class InProcessEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _gate = new();

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
        {
            var key = typeof(TEvent);
            if (!_handlers.TryGetValue(key, out var list))
            {
                list = [];
                _handlers[key] = list;
            }

            list.Add(handler);
        }
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        List<Delegate> handlers;
        lock (_gate)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list) || list.Count == 0)
            {
                return;
            }

            handlers = [.. list];
        }

        foreach (var handler in handlers)
        {
            var typed = (Func<TEvent, CancellationToken, Task>)handler;
            await typed(integrationEvent, cancellationToken);
        }
    }
}
