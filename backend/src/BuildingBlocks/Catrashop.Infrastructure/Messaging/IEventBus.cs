namespace Catrashop.Infrastructure.Messaging;

/// <summary>
/// Publishes a serialized integration event to the message broker. The Outbox publisher calls this for each
/// unprocessed row; a failure (e.g. the broker is down) throws so the row stays unprocessed and is retried.
/// </summary>
public interface IEventBus
{
    Task PublishAsync(string type, string jsonContent, Guid messageId, Guid? correlationId, CancellationToken ct = default);
}
