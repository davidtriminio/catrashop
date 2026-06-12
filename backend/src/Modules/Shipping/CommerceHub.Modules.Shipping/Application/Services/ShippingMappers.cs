using CommerceHub.Modules.Shipping.Application.Contracts;
using CommerceHub.Modules.Shipping.Domain;

namespace CommerceHub.Modules.Shipping.Application.Services;

internal static class ShippingMappers
{
    public static ShippingMethodResponse ToResponse(ShippingMethod m) =>
        new(m.Id, m.Code, m.Name, m.Cost, m.Currency, m.EstimatedDays, m.IsActive);

    public static ShipmentResponse ToResponse(Shipment s)
    {
        var d = s.Destination;
        return new ShipmentResponse(s.Id, s.OrderId, s.Carrier, s.TrackingNumber, s.MethodCode, s.Cost, s.Currency,
            s.Status.ToString(), d.RecipientName, d.City, d.Country, s.CreatedAtUtc, s.ShippedAtUtc, s.DeliveredAtUtc,
            Events(s));
    }

    public static TrackingResponse ToTracking(Shipment s) =>
        new(s.TrackingNumber, s.Carrier, s.Status.ToString(), s.Destination.City, s.Destination.Country, Events(s));

    private static IReadOnlyList<ShipmentEventResponse> Events(Shipment s) =>
        s.Events.OrderBy(e => e.OccurredAtUtc)
            .Select(e => new ShipmentEventResponse(e.Status.ToString(), e.Note, e.OccurredAtUtc))
            .ToArray();
}
