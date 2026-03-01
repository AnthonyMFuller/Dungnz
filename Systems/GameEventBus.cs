namespace Dungnz.Systems;

/// <summary>
/// A simple generic pub/sub event bus. Systems subscribe to event types
/// and are notified when those events are published. Thread-safe.
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
