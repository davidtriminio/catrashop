using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Shipping.Domain;

public static class ShippingErrors
{
    public static Error MethodNotFound(string code) =>
        Error.NotFound("shipping.method.not_found", $"Shipping method '{code}' was not found or is inactive.");

    public static Error MethodCodeInUse(string code) =>
        Error.Conflict("shipping.method.code_in_use", $"A shipping method with code '{code}' already exists.");

    public static readonly Error MethodNotFoundById =
        Error.NotFound("shipping.method.not_found", "Shipping method not found.");

    public static readonly Error OrderNotFound =
        Error.NotFound("shipping.order_not_found", "The order to ship was not found.");

    public static readonly Error OrderNotShippable =
        Error.Conflict("shipping.order_not_shippable", "The order is not in a state that can be shipped (must be paid).");

    public static readonly Error ShipmentAlreadyExists =
        Error.Conflict("shipping.shipment_exists", "A shipment already exists for this order.");

    public static Error ZoneCodeInUse(string code) =>
        Error.Conflict("shipping.zone.code_in_use", $"A shipping zone with code '{code}' already exists.");

    public static readonly Error ZoneNotFound =
        Error.NotFound("shipping.zone.not_found", "Shipping zone not found.");

    public static Error DepartmentsInUse(IEnumerable<string> codes) =>
        Error.Conflict("shipping.zone.department_in_use",
            $"These departments already belong to another zone: {string.Join(", ", codes)}.");

    public static readonly Error PickupStoreNotFound =
        Error.NotFound("shipping.pickup_store.not_found", "Pickup store not found.");

    public static readonly Error ShipmentNotFound =
        Error.NotFound("shipping.shipment.not_found", "Shipment not found.");

    public static Error InvalidTransition(ShipmentStatus from, ShipmentStatus to) =>
        Error.Conflict("shipping.invalid_transition", $"A shipment cannot move from {from} to {to}.");
}
