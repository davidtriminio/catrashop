using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Cart.Contracts;
using Catrashop.Modules.Inventory.Contracts;
using Catrashop.Modules.Orders.Application.Abstractions;
using Catrashop.Modules.Orders.Application.Configuration;
using Catrashop.Modules.Orders.Application.Contracts;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Orders.Domain;
using Catrashop.Modules.Promotions.Contracts;
using DeliveryMode = Catrashop.Modules.Orders.Domain.DeliveryMode;
using Microsoft.Extensions.Options;

namespace Catrashop.Modules.Orders.Application.Services;

/// <summary>
/// Orchestrates checkout (Section 8): read the cart, compute totals, reserve inventory, create the order, and
/// publish OrderCreated. Steps run against their own modules' units of work (there is no single distributed
/// transaction); consistency is preserved by ordering — inventory is reserved before the order is persisted, the
/// reservation is keyed on the cart id so retries are idempotent, and an unfinished checkout self-heals when the
/// reservation expires. Checkout itself is idempotent: a cart that already produced an order returns that order.
/// </summary>
public sealed class OrderCheckoutService
{
    private readonly ICartQuery _cart;
    private readonly IInventoryOperations _inventory;
    private readonly IPromotionApplication _promotions;
    private readonly IShippingQuotation _shipping;
    private readonly IOrderRepository _orders;
    private readonly IIntegrationEventPublisher _events;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly OrderingOptions _options;

    public OrderCheckoutService(
        ICartQuery cart,
        IInventoryOperations inventory,
        IPromotionApplication promotions,
        IShippingQuotation shipping,
        IOrderRepository orders,
        IIntegrationEventPublisher events,
        IOrderUnitOfWork unitOfWork,
        TimeProvider clock,
        IOptions<OrderingOptions> options)
    {
        _cart = cart;
        _inventory = inventory;
        _promotions = promotions;
        _shipping = shipping;
        _orders = orders;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<Result<CheckoutResponse>> CheckoutAsync(Guid customerId, CheckoutRequest request, CancellationToken ct = default)
    {
        var cart = await _cart.GetActiveCartAsync(customerId, ct);
        if (cart is null || cart.Lines.Count == 0)
            return OrderErrors.EmptyCart;

        // Idempotency: this cart may already have produced an order (e.g. a retried request).
        var existing = await _orders.GetByCartIdAsync(cart.CartId, ct);
        if (existing is not null)
        {
            await _cart.MarkCheckedOutAsync(cart.CartId, ct);
            return ToResponse(existing);
        }

        var now = _clock.GetUtcNow();

        // A coupon, if supplied, is priced (read-only) before the order is built so its discount reduces the
        // taxable base. An invalid coupon fails checkout with the coupon's own error — the customer can retry.
        var hasCoupon = !string.IsNullOrWhiteSpace(request.CouponCode);
        var discountTotal = 0m;
        if (hasCoupon)
        {
            var quote = await _promotions.QuoteAsync(request.CouponCode!, customerId, cart.Subtotal, cart.Currency, ct);
            if (quote.IsFailure)
                return quote.Error;
            discountTotal = quote.Value.DiscountAmount;
        }

        var taxTotal = Math.Round((cart.Subtotal - discountTotal) * (_options.TaxRatePercent / 100m), 2, MidpointRounding.AwayFromZero);

        // Where and how the order travels, and what that costs, is decided here from the server's own zones and
        // stores — never from anything the client sends beyond the department or store it picked.
        var destination = await ResolveDestinationAsync(request, cart.Currency, ct);
        if (destination.IsFailure)
            return destination.Error;
        var (shipping, delivery) = destination.Value;

        var lines = cart.Lines.Select(l => (l.ProductId, l.VariantId, l.Sku, l.ProductName, l.UnitPrice, l.Quantity));

        var orderResult = Order.Create(GenerateOrderNumber(now), customerId, cart.CartId, cart.Currency,
            shipping, lines, discountTotal, taxTotal, now, delivery);
        if (orderResult.IsFailure)
            return orderResult.Error;
        var order = orderResult.Value;

        // Reserve stock before persisting the order; if any line is short, nothing is created. Reserving first
        // (before redeeming the coupon) means the common failure — a line gone out of stock — never consumes a
        // coupon use.
        var stockLines = cart.Lines.Select(l => new StockRequest(l.Sku, l.Quantity)).ToArray();
        var reserve = await _inventory.ReserveAsync(cart.CartId, stockLines, TimeSpan.FromMinutes(_options.ReservationHoldMinutes), ct);
        if (reserve.IsFailure)
            return reserve.Error;

        // Commit the coupon redemption (idempotent on the cart id). Re-validates under the current usage counts,
        // so a coupon exhausted between the quote and here still fails closed; the reservation then self-expires.
        if (hasCoupon)
        {
            var redeem = await _promotions.RedeemAsync(request.CouponCode!, customerId, cart.CartId, cart.Subtotal, cart.Currency, ct);
            if (redeem.IsFailure)
                return redeem.Error;
        }

        var transition = order.MarkPaymentPending(now); // checkout initiates payment
        if (transition.IsFailure)
            return transition.Error;

        _orders.Add(order);
        _events.Enqueue(new OrderCreatedIntegrationEvent(order.Id, order.OrderNumber, order.CustomerId,
            order.Currency, order.GrandTotal, order.Items.Sum(i => i.Quantity)), order.Id);
        await _unitOfWork.SaveChangesAsync(ct);

        await _cart.MarkCheckedOutAsync(cart.CartId, ct);

        return ToResponse(order);
    }

    private async Task<Result<(Domain.ShippingAddress Address, OrderDelivery Delivery)>> ResolveDestinationAsync(
        CheckoutRequest request, string currency, CancellationToken ct)
    {
        Enum.TryParse<DeliveryMode>(request.DeliveryMode, ignoreCase: true, out var mode);

        if (mode == DeliveryMode.Pickup)
        {
            var store = request.PickupStoreId is { } id ? await _shipping.GetPickupStoreAsync(id, ct) : null;
            if (store is null)
                return OrderErrors.PickupStoreUnavailable;

            // The "address" of a pickup order is the store, so the order keeps a record of where it was collected.
            var address = new Domain.ShippingAddress(request.PickupRecipientName!.Trim(), store.Name, store.Line1, store.City,
                store.DepartmentName, "-", "HN");
            return (address, new OrderDelivery(DeliveryMode.Pickup, store.DepartmentCode, store.Id, store.Name, 0m));
        }

        var a = request.ShippingAddress!;
        var quote = await _shipping.QuoteDeliveryAsync(a.DepartmentCode ?? "", ct);
        if (quote is null)
            return OrderErrors.DeliveryNotAvailable;
        if (!string.Equals(quote.Currency, currency, StringComparison.OrdinalIgnoreCase))
            return OrderErrors.ShippingCurrencyMismatch;

        var delivered = new Domain.ShippingAddress(a.RecipientName.Trim(), a.Line1.Trim(), a.Line2?.Trim(), a.City.Trim(),
            quote.DepartmentName, a.PostalCode.Trim(), "HN");
        return (delivered, new OrderDelivery(DeliveryMode.Delivery, quote.DepartmentCode, null, null, quote.Cost));
    }

    private static CheckoutResponse ToResponse(Order order) =>
        new(order.Id, order.OrderNumber, order.Status.ToString(), order.Currency, order.GrandTotal);

    private static string GenerateOrderNumber(DateTimeOffset now) =>
        $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
