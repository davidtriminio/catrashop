namespace CommerceHub.Modules.Orders.Application.Contracts;

// --- Requests -----------------------------------------------------------------------------------

public sealed record ShippingAddressRequest(
    string RecipientName, string Line1, string? Line2, string City, string? Region, string PostalCode, string Country,
    string? DepartmentCode = null);

/// <summary>
/// <c>DeliveryMode</c> is "Delivery" (needs <c>ShippingAddress</c> with a department) or "Pickup" (needs a
/// <c>PickupStoreId</c> and the name of whoever collects the order). The shipping cost is decided by the server.
/// </summary>
public sealed record CheckoutRequest(
    ShippingAddressRequest? ShippingAddress, string? CouponCode = null, string DeliveryMode = "Delivery",
    Guid? PickupStoreId = null, string? PickupRecipientName = null);

public sealed record CancelOrderRequest(string? Reason);

public sealed record CheckoutResponse(Guid OrderId, string OrderNumber, string Status, string Currency, decimal GrandTotal);

// --- Responses ----------------------------------------------------------------------------------

public sealed record OrderItemResponse(
    Guid Id, Guid ProductId, Guid VariantId, string Sku, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderStatusHistoryResponse(string Status, string? Note, DateTimeOffset OccurredAtUtc);

public sealed record OrderSummaryResponse(
    Guid Id, string OrderNumber, string Status, string Currency, decimal GrandTotal, int ItemCount, DateTimeOffset CreatedAtUtc);

public sealed record OrderDetailResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal ShippingTotal,
    decimal GrandTotal,
    string DeliveryMode,
    Guid? PickupStoreId,
    string? PickupStoreName,
    ShippingAddressRequest ShippingAddress,
    IReadOnlyList<OrderItemResponse> Items,
    IReadOnlyList<OrderStatusHistoryResponse> History,
    DateTimeOffset CreatedAtUtc);
