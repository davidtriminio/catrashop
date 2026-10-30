using System.Text.Json;
using Catrashop.BuildingBlocks.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Catrashop.Infrastructure.Messaging;

/// <summary>
/// Turns a received broker message (type name + JSON) back into an event and invokes every registered
/// <see cref="IIntegrationEventHandler{TEvent}"/> for it, resolved from the given scope. An unknown type is
/// logged and ignored (so it isn't requeued forever); a handler that throws propagates so the consumer can
/// nack and let the broker redeliver.
/// </summary>
public sealed class IntegrationEventDispatcher
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly IntegrationEventTypeRegistry _registry;
    private readonly ILogger<IntegrationEventDispatcher> _logger;

    public IntegrationEventDispatcher(IntegrationEventTypeRegistry registry, ILogger<IntegrationEventDispatcher> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task DispatchAsync(string typeName, string jsonContent, IServiceProvider scope, CancellationToken ct)
    {
        if (!_registry.TryResolve(typeName, out var eventType))
        {
            _logger.LogWarning("Received unknown integration event type '{Type}'; ignoring.", typeName);
            return;
        }

        if (JsonSerializer.Deserialize(jsonContent, eventType, Json) is not IIntegrationEvent integrationEvent)
        {
            _logger.LogWarning("Could not deserialize integration event '{Type}'; ignoring.", typeName);
            return;
        }

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = scope.GetServices(handlerType).ToArray();
        if (handlers.Length == 0)
        {
            _logger.LogDebug("No handlers registered for {Type}.", typeName);
            return;
        }

        var handle = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!;
        foreach (var handler in handlers)
        {
            await (Task)handle.Invoke(handler, [integrationEvent, ct])!;
        }
    }
}
