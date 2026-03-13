namespace CommerceHub.BuildingBlocks.Domain;

/// <summary>
/// Consistency boundary of the domain. Only aggregate roots are loaded and saved as a unit, and
/// only they raise domain events. Invariants that must always hold together live behind a single root.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot(TId id) : base(id) { }
    protected AggregateRoot() { }

    /// <summary>Events raised since the aggregate was loaded, dispatched after a successful save.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
