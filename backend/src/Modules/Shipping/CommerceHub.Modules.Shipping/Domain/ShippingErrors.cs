using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Shipping.Domain;

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

    public static readonly Error ShipmentNotFound =
        Error.NotFound("shipping.shipment.not_found", "Shipment not found.");

    public static Error InvalidTransition(ShipmentStatus from, ShipmentStatus to) =>
        Error.Conflict("shipping.invalid_transition", $"A shipment cannot move from {from} to {to}.");
}
