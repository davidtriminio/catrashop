using CommerceHub.Modules.Cart.Application.Contracts;

namespace CommerceHub.Modules.Cart.Application.Services;

internal static class CartMappers
{
    public static CartResponse ToResponse(Domain.Cart cart) => new(
        cart.Id,
        cart.Currency,
        cart.Items
            .Select(i => new CartItemResponse(i.Id, i.ProductId, i.VariantId, i.Sku, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal))
            .ToArray(),
        cart.ItemCount,
        cart.Subtotal,
        cart.ExpiresAtUtc);
}
