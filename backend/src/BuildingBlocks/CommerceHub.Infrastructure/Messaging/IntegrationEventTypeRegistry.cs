using System.Collections.Concurrent;
using CommerceHub.BuildingBlocks.Messaging;

namespace CommerceHub.Infrastructure.Messaging;

/// <summary>
/// Maps an integration event's stable type name (its CLR full name, used as the broker routing key) to the CLR
/// type, so the consumer can deserialize a message and locate its handlers. Populated at startup from the event
/// contracts the host knows about; the consumer binds its queue to exactly these routing keys.
/// </summary>
public sealed class IntegrationEventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> _byName = new(StringComparer.Ordinal);

    public void Register(Type eventType)
    {
        if (!typeof(IIntegrationEvent).IsAssignableFrom(eventType))
            throw new ArgumentException($"{eventType} is not an {nameof(IIntegrationEvent)}.", nameof(eventType));
        _byName[eventType.FullName!] = eventType;
    }

    public void Register<TEvent>() where TEvent : IIntegrationEvent => Register(typeof(TEvent));

    public bool TryResolve(string typeName, out Type eventType)
    {
        var found = _byName.TryGetValue(typeName, out var type);
        eventType = type!;
        return found;
    }

    /// <summary>The registered type names — the routing keys the consumer queue binds to.</summary>
    public IReadOnlyCollection<string> Names => _byName.Keys.ToArray();
}
