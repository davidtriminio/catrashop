using Catrashop.BuildingBlocks.Domain;
using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Shipping.Domain;

/// <summary>
/// A shipment fulfilling an order (one per order). Carries the carrier, a unique tracking number, the captured
/// method cost, and the destination snapshot. Status moves only through <see cref="ShipmentStateMachine"/>, and
/// every change appends a tracking event.
/// </summary>
public sealed class Shipment : AggregateRoot<Guid>
{
    private readonly List<ShipmentEvent> _events = new();

    private Shipment() { } // EF

    private Shipment(Guid id, Guid orderId, string carrier, string trackingNumber, string methodCode,
        decimal cost, string currency, Destination destination, DateTimeOffset nowUtc) : base(id)
    {
        OrderId = orderId;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
        MethodCode = methodCode;
        Cost = cost;
        Currency = currency;
        Destination = destination;
        Status = ShipmentStatus.Pending;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public Guid OrderId { get; private set; }
    public string Carrier { get; private set; } = default!;
    public string TrackingNumber { get; private set; } = default!;
    public string MethodCode { get; private set; } = default!;
    public decimal Cost { get; private set; }
    public string Currency { get; private set; } = default!;
    public ShipmentStatus Status { get; private set; }
    public Destination Destination { get; private set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ShippedAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public IReadOnlyCollection<ShipmentEvent> Events => _events.AsReadOnly();

    public static Shipment Create(Guid orderId, string carrier, string trackingNumber, string methodCode,
        decimal cost, string currency, Destination destination, DateTimeOffset nowUtc)
    {
        var shipment = new Shipment(Guid.NewGuid(), orderId, carrier.Trim(), trackingNumber, methodCode,
            cost, currency.ToUpperInvariant(), destination, nowUtc);
        shipment.RecordEvent(ShipmentStatus.Pending, "Shipment created", nowUtc);
        return shipment;
    }

    public Result MarkShipped(DateTimeOffset nowUtc)
    {
        var t = Transition(ShipmentStatus.Shipped, "Handed to carrier", nowUtc);
        if (t.IsSuccess) ShippedAtUtc = nowUtc;
        return t;
    }

    public Result MarkDelivered(DateTimeOffset nowUtc)
    {
        var t = Transition(ShipmentStatus.Delivered, "Delivered", nowUtc);
        if (t.IsSuccess) DeliveredAtUtc = nowUtc;
        return t;
    }

    public Result Cancel(string? reason, DateTimeOffset nowUtc) =>
        Transition(ShipmentStatus.Cancelled, reason ?? "Cancelled", nowUtc);

    private Result Transition(ShipmentStatus to, string? note, DateTimeOffset nowUtc)
    {
        if (!ShipmentStateMachine.CanTransition(Status, to))
            return ShippingErrors.InvalidTransition(Status, to);

        Status = to;
        RecordEvent(to, note, nowUtc);
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    private void RecordEvent(ShipmentStatus status, string? note, DateTimeOffset nowUtc) =>
        _events.Add(new ShipmentEvent(Guid.NewGuid(), Id, status, note, nowUtc));
}
