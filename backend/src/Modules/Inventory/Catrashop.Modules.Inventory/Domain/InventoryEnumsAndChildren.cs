namespace Catrashop.Modules.Inventory.Domain;

/// <summary>Kind of stock movement, for the audit trail.</summary>
public enum MovementType
{
    Received = 0,   // stock arrived (on-hand +)
    Adjusted = 1,   // manual correction (on-hand + or -)
    Reserved = 2,   // held for an order (reserved +)
    Released = 3,   // reservation cancelled/expired (reserved -)
    Shipped = 4     // fulfilled: units left stock (on-hand - and reserved -)
}

/// <summary>Lifecycle of a stock reservation.</summary>
public enum ReservationStatus
{
    Held = 0,
    Released = 1,
    Committed = 2
}

/// <summary>
/// An append-only audit record of a change to an inventory item. <see cref="Quantity"/> is the
/// magnitude; interpret its effect on on-hand/reserved by <see cref="Type"/>.
/// </summary>
public sealed class StockMovement
{
    private StockMovement() { } // EF

    internal StockMovement(Guid id, Guid itemId, MovementType type, int quantity, string? reason, Guid? reference, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        ItemId = itemId;
        Type = type;
        Quantity = quantity;
        Reason = reason;
        Reference = reference;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }
    public MovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string? Reason { get; private set; }

    /// <summary>Correlates reserve/release/ship movements with the reservation that caused them.</summary>
    public Guid? Reference { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}

/// <summary>
/// A hold on stock for a specific reservation reference (typically an order/checkout id). The
/// <see cref="Reference"/> is the idempotency key: reserving twice with the same reference is a no-op.
/// </summary>
public sealed class Reservation
{
    private Reservation() { } // EF

    internal Reservation(Guid id, Guid itemId, Guid reference, int quantity, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        Id = id;
        ItemId = itemId;
        Reference = reference;
        Quantity = quantity;
        Status = ReservationStatus.Held;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid Reference { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public bool IsHeld => Status == ReservationStatus.Held;
    public bool IsExpired(DateTimeOffset nowUtc) => IsHeld && nowUtc >= ExpiresAtUtc;

    internal void MarkReleased() => Status = ReservationStatus.Released;
    internal void MarkCommitted() => Status = ReservationStatus.Committed;
}
