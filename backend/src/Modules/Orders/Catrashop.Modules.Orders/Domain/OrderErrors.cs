using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Orders.Domain;

public static class OrderErrors
{
    public static readonly Error NotFound =
        Error.NotFound("orders.not_found", "Order not found.");

    public static readonly Error EmptyCart =
        Error.Validation("orders.empty_cart", "Your cart is empty; there is nothing to check out.");

    public static Error InvalidTransition(OrderStatus from, OrderStatus to) =>
        Error.Conflict("orders.invalid_transition", $"An order cannot move from {from} to {to}.");

    public static readonly Error DeliveryNotAvailable =
        Error.Validation("orders.delivery.not_available", "We do not deliver to that department.");

    public static readonly Error PickupStoreUnavailable =
        Error.Validation("orders.pickup.store_unavailable", "The selected pickup store is not available.");

    public static readonly Error ShippingCurrencyMismatch =
        Error.Validation("orders.delivery.currency_mismatch", "The shipping cost is not in the currency of the order.");

    public static readonly Error NoLines =
        Error.Validation("orders.no_lines", "An order must have at least one line.");
}
