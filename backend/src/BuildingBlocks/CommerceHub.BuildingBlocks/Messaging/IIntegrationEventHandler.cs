namespace CommerceHub.BuildingBlocks.Messaging;

/// <summary>
/// Handles an integration event delivered from the message broker. Handlers run asynchronously after the
/// publishing module has committed, on the consumer side of the Outbox → broker → inbox flow. Delivery is
/// at-least-once, so <b>handlers must be idempotent</b> (key on <see cref="IIntegrationEvent.EventId"/> or use
/// get-or-create semantics): the same event may be delivered more than once.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken ct = default);
}
