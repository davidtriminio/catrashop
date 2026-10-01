using Microsoft.Extensions.DependencyInjection;

namespace CommerceHub.Infrastructure.Messaging;

/// <summary>
/// Broker-less <see cref="IEventBus"/>: dispatches each outbox event directly to its handlers in a fresh DI scope.
/// Selected with <c>Messaging:Transport=InProcess</c> for low-memory single-node hosts. The Outbox still gives
/// at-least-once delivery: if a handler throws, the row stays unprocessed and is retried on the next cycle.
/// </summary>
public sealed class InProcessEventBus : IEventBus
{
    private readonly IServiceProvider _services;
    private readonly IntegrationEventDispatcher _dispatcher;

    public InProcessEventBus(IServiceProvider services, IntegrationEventDispatcher dispatcher)
    {
        _services = services;
        _dispatcher = dispatcher;
    }

    public async Task PublishAsync(string type, string jsonContent, Guid messageId, Guid? correlationId, CancellationToken ct = default)
    {
        await using var scope = _services.CreateAsyncScope();
        await _dispatcher.DispatchAsync(type, jsonContent, scope.ServiceProvider, ct);
    }
}
