using Catrashop.BuildingBlocks.Domain;
using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Inventory.Domain;

/// <summary>
/// Tracks stock for a single SKU. Distinguishes <see cref="QuantityOnHand"/> (physical units) from
/// <see cref="QuantityReserved"/> (held for pending orders); <see cref="AvailableQuantity"/> is what a
/// new order may take. Enforces the core inventory invariants that prevent overselling:
/// reserved never exceeds on-hand, and available never goes negative. Reservations are idempotent by
/// their reference (the order/checkout id), so retries don't double-reserve or double-release.
/// </summary>
public sealed class InventoryItem : AggregateRoot<Guid>
{
    private readonly List<StockMovement> _movements = new();
    private readonly List<Reservation> _reservations = new();

    private InventoryItem() { } // EF

    private InventoryItem(Guid id, string sku, int onHand, int lowStockThreshold, DateTimeOffset nowUtc) : base(id)
    {
        Sku = sku;
        QuantityOnHand = onHand;
        QuantityReserved = 0;
        LowStockThreshold = lowStockThreshold;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string Sku { get; private set; } = default!;
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }
    public int LowStockThreshold { get; private set; }
    public bool LowStockNotified { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Units a new order may reserve: on-hand minus already reserved.</summary>
    public int AvailableQuantity => QuantityOnHand - QuantityReserved;

    public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();
    public IReadOnlyCollection<Reservation> Reservations => _reservations.AsReadOnly();

    public static InventoryItem Create(string sku, int initialOnHand, int lowStockThreshold, DateTimeOffset nowUtc)
    {
        var item = new InventoryItem(Guid.NewGuid(), sku.Trim().ToUpperInvariant(), Math.Max(0, initialOnHand), Math.Max(0, lowStockThreshold), nowUtc);
        if (initialOnHand > 0)
            item.AddMovement(MovementType.Received, initialOnHand, "Initial stock", null, nowUtc);
        return item;
    }

    public Result Receive(int quantity, string? reason, DateTimeOffset nowUtc)
    {
        if (quantity <= 0)
            return InventoryErrors.NegativeQuantity;

        QuantityOnHand += quantity;
        AddMovement(MovementType.Received, quantity, reason, null, nowUtc);
        Touch(nowUtc);
        return Result.Success();
    }

    /// <summary>Manual correction. <paramref name="delta"/> may be negative but must not drop on-hand below reserved.</summary>
    public Result Adjust(int delta, string? reason, DateTimeOffset nowUtc)
    {
        if (delta == 0)
            return InventoryErrors.NegativeQuantity;

        var newOnHand = QuantityOnHand + delta;
        if (newOnHand < QuantityReserved)
            return InventoryErrors.AdjustmentBelowReserved;

        QuantityOnHand = newOnHand;
        AddMovement(MovementType.Adjusted, delta, reason, null, nowUtc);
        Touch(nowUtc);
        return Result.Success();
    }

    public void SetLowStockThreshold(int threshold, DateTimeOffset nowUtc)
    {
        LowStockThreshold = Math.Max(0, threshold);
        Touch(nowUtc);
    }

    public Result Reserve(Guid reference, int quantity, DateTimeOffset nowUtc, DateTimeOffset expiresAtUtc)
    {
        if (quantity <= 0)
            return InventoryErrors.NegativeQuantity;

        // Idempotent: an existing held reservation with the same reference is a no-op success.
        if (_reservations.Any(r => r.Reference == reference && r.IsHeld))
            return Result.Success();

        if (AvailableQuantity < quantity)
            return InventoryErrors.InsufficientStock(Sku, quantity, AvailableQuantity);

        QuantityReserved += quantity;
        _reservations.Add(new Reservation(Guid.NewGuid(), Id, reference, quantity, nowUtc, expiresAtUtc));
        AddMovement(MovementType.Reserved, quantity, null, reference, nowUtc);
        Touch(nowUtc);
        return Result.Success();
    }

    public Result Release(Guid reference, DateTimeOffset nowUtc)
    {
        var reservation = _reservations.FirstOrDefault(r => r.Reference == reference && r.IsHeld);
        if (reservation is null)
            return Result.Success(); // idempotent: already released/committed or never existed

        QuantityReserved -= reservation.Quantity;
        reservation.MarkReleased();
        AddMovement(MovementType.Released, reservation.Quantity, null, reference, nowUtc);
        Touch(nowUtc);
        return Result.Success();
    }

    /// <summary>Fulfill a held reservation: units leave stock (on-hand and reserved both decrease).</summary>
    public Result Commit(Guid reference, DateTimeOffset nowUtc)
    {
        var reservation = _reservations.FirstOrDefault(r => r.Reference == reference && r.IsHeld);
        if (reservation is null)
            return Result.Success(); // idempotent: already committed

        QuantityOnHand -= reservation.Quantity;
        QuantityReserved -= reservation.Quantity;
        reservation.MarkCommitted();
        AddMovement(MovementType.Shipped, reservation.Quantity, null, reference, nowUtc);
        Touch(nowUtc);
        return Result.Success();
    }

    /// <summary>Release any reservations that have passed their expiry (called by a background sweep).</summary>
    public int ReleaseExpiredReservations(DateTimeOffset nowUtc)
    {
        var released = 0;
        foreach (var reservation in _reservations.Where(r => r.IsExpired(nowUtc)).ToList())
        {
            QuantityReserved -= reservation.Quantity;
            reservation.MarkReleased();
            AddMovement(MovementType.Released, reservation.Quantity, "Expired", reservation.Reference, nowUtc);
            released++;
        }
        if (released > 0) Touch(nowUtc);
        return released;
    }

    /// <summary>
    /// Returns true exactly once each time available stock crosses down to/through the threshold, so the
    /// caller emits a single LowInventoryDetected event per crossing rather than on every subsequent change.
    /// </summary>
    public bool ShouldNotifyLowStock()
    {
        if (LowStockThreshold <= 0)
            return false;

        if (AvailableQuantity <= LowStockThreshold)
        {
            if (LowStockNotified)
                return false;
            LowStockNotified = true;
            return true;
        }

        LowStockNotified = false;
        return false;
    }

    private void AddMovement(MovementType type, int quantity, string? reason, Guid? reference, DateTimeOffset nowUtc) =>
        _movements.Add(new StockMovement(Guid.NewGuid(), Id, type, quantity, reason, reference, nowUtc));

    private void Touch(DateTimeOffset nowUtc) => UpdatedAtUtc = nowUtc;
}
