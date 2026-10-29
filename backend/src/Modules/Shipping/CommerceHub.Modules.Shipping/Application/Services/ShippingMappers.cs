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

    public static ShippingZoneResponse ToResponse(ShippingZone zone) =>
        new(zone.Id, zone.Code, zone.Name, zone.Cost, zone.Currency, zone.EstimatedDaysMin, zone.EstimatedDaysMax,
            zone.CashOnDeliveryCovered, zone.IsActive,
            zone.Departments
                .Select(d => HondurasDepartments.Find(d.DepartmentCode) ?? new Department(d.DepartmentCode, d.DepartmentCode))
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentResponse(d.Code, d.Name))
                .ToArray());

    public static PickupStoreResponse ToResponse(PickupStore store) =>
        new(store.Id, store.Name, store.Line1, store.City, store.DepartmentCode,
            HondurasDepartments.Find(store.DepartmentCode)?.Name ?? store.DepartmentCode,
            store.Phone, store.Hours, store.IsActive);
}
