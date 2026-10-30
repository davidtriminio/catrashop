namespace Catrashop.Modules.Cart.Application.Contracts;

// --- Requests -----------------------------------------------------------------------------------

public sealed record AddCartItemRequest(Guid VariantId, int Quantity);

public sealed record UpdateCartItemRequest(int Quantity);

// --- Responses ----------------------------------------------------------------------------------

public sealed record CartItemResponse(
    Guid Id,
    Guid ProductId,
    Guid VariantId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record CartResponse(
    Guid Id,
    string Currency,
    IReadOnlyList<CartItemResponse> Items,
    int ItemCount,
    decimal Subtotal,
    DateTimeOffset ExpiresAtUtc);
