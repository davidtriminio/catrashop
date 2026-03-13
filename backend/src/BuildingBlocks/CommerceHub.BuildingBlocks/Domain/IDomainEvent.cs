namespace CommerceHub.BuildingBlocks.Domain;

/// <summary>
/// Something that happened inside an aggregate that other parts of the *same* module may react to
/// synchronously. Domain events are raised on the aggregate and dispatched after the change is
/// persisted. Cross-module communication uses integration events (via the Outbox), not these.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAtUtc { get; }
}

/// <summary>Convenience base assigning an id and timestamp at construction.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
