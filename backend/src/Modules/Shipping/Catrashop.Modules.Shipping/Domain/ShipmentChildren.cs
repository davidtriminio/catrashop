namespace Catrashop.Modules.Shipping.Domain;

/// <summary>Shipment lifecycle. Guarded by <see cref="ShipmentStateMachine"/>.</summary>
public enum ShipmentStatus
{
    Pending = 0,    // created, label ready, not yet handed to carrier
    Shipped = 1,    // in transit
    Delivered = 2,  // received
    Cancelled = 3   // cancelled before shipping
}

public static class ShipmentStateMachine
{
    private static readonly IReadOnlyDictionary<ShipmentStatus, IReadOnlySet<ShipmentStatus>> Allowed =
        new Dictionary<ShipmentStatus, IReadOnlySet<ShipmentStatus>>
        {
            [ShipmentStatus.Pending] = Set(ShipmentStatus.Shipped, ShipmentStatus.Cancelled),
            [ShipmentStatus.Shipped] = Set(ShipmentStatus.Delivered),
            [ShipmentStatus.Delivered] = Set(),
            [ShipmentStatus.Cancelled] = Set()
        };

    public static bool CanTransition(ShipmentStatus from, ShipmentStatus to) =>
        Allowed.TryGetValue(from, out var t) && t.Contains(to);

    private static IReadOnlySet<ShipmentStatus> Set(params ShipmentStatus[] s) => new HashSet<ShipmentStatus>(s);
}

/// <summary>A tracking event on a shipment, forming its timeline.</summary>
public sealed class ShipmentEvent
{
    private ShipmentEvent() { } // EF

    internal ShipmentEvent(Guid id, Guid shipmentId, ShipmentStatus status, string? note, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        ShipmentId = shipmentId;
        Status = status;
        Note = note;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ShipmentId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}

/// <summary>Destination snapshot copied from the order at shipment creation.</summary>
public sealed class Destination
{
    private Destination() { } // EF

    public Destination(string recipientName, string line1, string? line2, string city, string? region, string postalCode, string country)
    {
        RecipientName = recipientName;
        Line1 = line1;
        Line2 = line2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
    }

    public string RecipientName { get; private set; } = default!;
    public string Line1 { get; private set; } = default!;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = default!;
    public string? Region { get; private set; }
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;
}
