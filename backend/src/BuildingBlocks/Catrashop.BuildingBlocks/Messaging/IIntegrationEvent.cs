namespace Catrashop.BuildingBlocks.Messaging;

/// <summary>
/// A fact published from one module for *other* modules (or external systems) to react to,
/// asynchronously and after commit. Unlike a domain event (in-module, synchronous), an integration
/// event crosses boundaries and travels through the Outbox → message broker. Keep its shape a
/// stable contract: other modules depend on it.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>Convenience base that assigns an id and timestamp at construction.</summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
