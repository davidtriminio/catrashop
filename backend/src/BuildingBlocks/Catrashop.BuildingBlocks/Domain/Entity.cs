namespace Catrashop.BuildingBlocks.Domain;

/// <summary>
/// Base for domain entities: identity-based equality (two entities are the same if their ids match,
/// regardless of field values). <typeparamref name="TId"/> is the strongly-typed identifier.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    // EF Core materialization ctor.
    protected Entity() => Id = default!;

    public TId Id { get; protected set; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && other.GetType() == GetType() && other.Id.Equals(Id);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
