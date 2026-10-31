namespace Catrashop.Modules.Orders.Domain;

/// <summary>How the customer receives the order: delivered to an address, or collected at a pickup store.</summary>
public enum DeliveryMode
{
    Delivery = 0,
    Pickup = 1,
}

/// <summary>
/// The delivery choices captured on an order at checkout. The shipping total is computed by the server from the
/// destination zone; <see cref="None"/> (no shipping charge) is what orders created before national shipping carry.
/// </summary>
public sealed record OrderDelivery(
    DeliveryMode Mode, string? DepartmentCode, Guid? PickupStoreId, string? PickupStoreName, decimal ShippingTotal)
{
    public static readonly OrderDelivery None = new(DeliveryMode.Delivery, null, null, null, 0m);
}
