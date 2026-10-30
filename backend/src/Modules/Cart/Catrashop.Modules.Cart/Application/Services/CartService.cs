using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Cart.Application.Abstractions;
using Catrashop.Modules.Cart.Application.Contracts;
using Catrashop.Modules.Cart.Contracts;
using Catrashop.Modules.Cart.Domain;
using Catrashop.Modules.Catalog.Contracts;
using Catrashop.Modules.Inventory.Contracts;
using Microsoft.Extensions.Options;

namespace Catrashop.Modules.Cart.Application.Services;

/// <summary>
/// Cart operations for the authenticated customer, plus the public <see cref="ICartQuery"/> used by checkout.
/// Prices and names are captured from the Catalog at add-time via <see cref="IProductCatalogQuery"/> so the
/// client can never dictate a price. There is one active cart per customer; an expired cart is reset on access.
/// </summary>
public sealed class CartService : ICartQuery
{
    private readonly ICartRepository _carts;
    private readonly IProductCatalogQuery _catalog;
    private readonly ICartUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly IStockQuery _stock;
    private readonly CartOptions _options;

    public CartService(ICartRepository carts, IProductCatalogQuery catalog, ICartUnitOfWork unitOfWork, TimeProvider clock,
        IStockQuery stock, IOptions<CartOptions> options)
    {
        _carts = carts;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _stock = stock;
        _options = options.Value;
    }

    public async Task<CartResponse> GetAsync(Guid customerId, CancellationToken ct = default)
    {
        var (cart, changed) = await GetOrCreateAsync(customerId, ct);
        if (changed)
            await _unitOfWork.SaveChangesAsync(ct);
        return CartMappers.ToResponse(cart);
    }

    public async Task<Result<CartResponse>> AddItemAsync(Guid customerId, AddCartItemRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            return CartErrors.QuantityMustBePositive;

        var variant = await _catalog.GetPurchasableVariantAsync(request.VariantId, ct);
        if (variant is null)
            return CartErrors.VariantNotPurchasable;

        var (cart, _) = await GetOrCreateAsync(customerId, ct);

        var inCart = cart.Items.FirstOrDefault(i => i.VariantId == variant.VariantId)?.Quantity ?? 0;
        var check = CartQuantityPolicy.Check(inCart + request.Quantity, _options.MaxQuantityPerLine,
            await _stock.GetAvailableAsync(variant.Sku, ct));
        if (check.IsFailure)
            return check.Error;

        var result = cart.AddItem(variant.ProductId, variant.VariantId, variant.Sku, variant.ProductName,
            variant.UnitPrice, variant.Currency, request.Quantity, _clock.GetUtcNow());
        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(ct);
        return CartMappers.ToResponse(cart);
    }

    public async Task<Result<CartResponse>> UpdateItemAsync(Guid customerId, Guid itemId, UpdateCartItemRequest request, CancellationToken ct = default)
    {
        if (request.Quantity > 0)
        {
            var cart = await _carts.GetActiveByCustomerAsync(customerId, ct);
            var item = cart?.Items.FirstOrDefault(i => i.Id == itemId);
            if (item is not null)
            {
                var check = CartQuantityPolicy.Check(request.Quantity, _options.MaxQuantityPerLine,
                    await _stock.GetAvailableAsync(item.Sku, ct));
                if (check.IsFailure)
                    return check.Error;
            }
        }

        return await MutateAsync(customerId, cart => cart.SetItemQuantity(itemId, request.Quantity, _clock.GetUtcNow()), ct);
    }

    public Task<Result<CartResponse>> RemoveItemAsync(Guid customerId, Guid itemId, CancellationToken ct = default) =>
        MutateAsync(customerId, cart => cart.RemoveItem(itemId, _clock.GetUtcNow()), ct);

    public async Task<Result<CartResponse>> ClearAsync(Guid customerId, CancellationToken ct = default)
    {
        var cart = await _carts.GetActiveByCustomerAsync(customerId, ct);
        if (cart is null)
            return CartMappers.ToResponse(Domain.Cart.Create(customerId, _clock.GetUtcNow())); // nothing to clear

        cart.Clear(_clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return CartMappers.ToResponse(cart);
    }

    // --- ICartQuery (consumed by Orders during checkout) ---------------------------------------

    public async Task<CartSnapshot?> GetActiveCartAsync(Guid customerId, CancellationToken ct = default)
    {
        var cart = await _carts.GetActiveByCustomerAsync(customerId, ct);
        if (cart is null || cart.IsExpired(_clock.GetUtcNow()))
            return null;

        return new CartSnapshot(cart.Id, cart.CustomerId, cart.Currency,
            cart.Items.Select(i => new CartLineSnapshot(i.ProductId, i.VariantId, i.Sku, i.ProductName, i.UnitPrice, i.Quantity)).ToArray(),
            cart.Subtotal);
    }

    public async Task MarkCheckedOutAsync(Guid cartId, CancellationToken ct = default)
    {
        var cart = await _carts.GetByIdAsync(cartId, ct);
        if (cart is null)
            return;

        cart.MarkCheckedOut(_clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // --- Helpers --------------------------------------------------------------------------------

    private async Task<Result<CartResponse>> MutateAsync(Guid customerId, Func<Domain.Cart, Result> mutate, CancellationToken ct)
    {
        var cart = await _carts.GetActiveByCustomerAsync(customerId, ct);
        if (cart is null)
            return CartErrors.ItemNotFound; // no active cart → nothing to mutate

        var result = mutate(cart);
        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(ct);
        return CartMappers.ToResponse(cart);
    }

    /// <summary>Returns the customer's active cart, creating it or resetting an expired one; the flag says whether it changed.</summary>
    private async Task<(Domain.Cart Cart, bool Changed)> GetOrCreateAsync(Guid customerId, CancellationToken ct)
    {
        var cart = await _carts.GetActiveByCustomerAsync(customerId, ct);
        if (cart is null)
        {
            cart = Domain.Cart.Create(customerId, _clock.GetUtcNow());
            _carts.Add(cart);
            return (cart, true);
        }

        if (cart.IsExpired(_clock.GetUtcNow()))
        {
            cart.Clear(_clock.GetUtcNow()); // recover an abandoned/expired cart as a fresh empty one
            return (cart, true);
        }

        return (cart, false);
    }
}
