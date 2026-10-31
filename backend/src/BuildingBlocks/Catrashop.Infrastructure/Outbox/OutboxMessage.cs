namespace Catrashop.Infrastructure.Outbox;

/// <summary>
/// A durable record of an integration event, written in the *same transaction* as the domain change
/// that produced it (the Outbox pattern). A background publisher later reads unprocessed rows and
/// pushes them to the broker, so an event is never lost even if the broker is down at commit time,
/// and never published without its domain change having committed. See ADR-005.
/// </summary>
public sealed class OutboxMessage
{
    // EF materialization.
    private OutboxMessage() { }

    public OutboxMessage(Guid id, string type, string content, DateTimeOffset occurredOnUtc, Guid? correlationId)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
        CorrelationId = correlationId;
    }

    public Guid Id { get; private set; }

    /// <summary>Logical event type name used to resolve the concrete type on publish.</summary>
    public string Type { get; private set; } = default!;

    /// <summary>Serialized (JSON) event payload.</summary>
    public string Content { get; private set; } = default!;

    public DateTimeOffset OccurredOnUtc { get; private set; }

    /// <summary>Null until successfully published; set to prevent re-publishing (idempotency).</summary>
    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    /// <summary>Last failure detail, if any, for diagnostics and dead-lettering decisions.</summary>
    public string? Error { get; private set; }

    public int RetryCount { get; private set; }

    /// <summary>Ties the event back to the originating request for end-to-end tracing.</summary>
    public Guid? CorrelationId { get; private set; }

    public void MarkProcessed(DateTimeOffset whenUtc)
    {
        ProcessedOnUtc = whenUtc;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
    }
}
