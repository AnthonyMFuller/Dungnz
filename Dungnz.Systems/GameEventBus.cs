namespace Dungnz.Systems;

/// <summary>
/// A generic, type-safe pub/sub event bus for decoupled inter-system communication.
/// Subscribers register handlers by event type; publishers broadcast events to all
/// matching handlers. Thread-safe. This bus uses the <see cref="IGameEvent"/> marker
/// interface and is available as reusable infrastructure — the primary game loop uses
/// <see cref="GameEvents"/> (a domain-specific C# event hub) for its runtime events.
/// </summary>
public class GameEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Subscribes a handler to be invoked whenever an event of type
    /// <typeparamref name="T"/> is published.
    /// </summary>
    /// <typeparam name="T">The event type to listen for.</typeparam>
    /// <param name="handler">The handler to invoke on publish.</param>
    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type))
                _handlers[type] = new List<Delegate>();
            _handlers[type].Add(handler);
        }
    }

    /// <summary>
    /// Publishes an event to all registered handlers of type <typeparamref name="T"/>.
    /// Handlers are invoked synchronously in registration order.
    /// </summary>
    /// <typeparam name="T">The event type being published.</typeparam>
    /// <param name="evt">The event instance to publish.</param>
    public void Publish<T>(T evt) where T : IGameEvent
    {
        List<Delegate>? handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out handlers))
                return;
            handlers = new List<Delegate>(handlers);
        }

        foreach (var handler in handlers)
        {
            if (handler is Action<T> typed)
                typed(evt);
        }
    }

    /// <summary>
    /// Unsubscribes a previously registered handler for event type <typeparamref name="T"/>.
    /// If the handler was not registered, this is a no-op.
    /// </summary>
    /// <typeparam name="T">The event type to stop listening for.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
                list.Remove(handler);
        }
    }

    /// <summary>
    /// Removes all subscriptions. Used for testing or between game sessions.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _handlers.Clear();
        }
    }

    /// <summary>Gets the total number of registered handlers across all event types.</summary>
    public int HandlerCount
    {
        get
        {
            lock (_lock) { return _handlers.Values.Sum(h => h.Count); }
        }
    }
}
