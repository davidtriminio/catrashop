using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Cart.Contracts;
using CommerceHub.Modules.Inventory.Contracts;
using CommerceHub.Modules.Orders.Application.Abstractions;
using CommerceHub.Modules.Orders.Application.Configuration;
using CommerceHub.Modules.Orders.Application.Contracts;
using CommerceHub.Modules.Orders.Contracts;
using CommerceHub.Modules.Orders.Domain;
using Microsoft.Extensions.Options;

namespace CommerceHub.Modules.Orders.Application.Services;

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
    private readonly IOrderRepository _orders;
    private readonly IIntegrationEventPublisher _events;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly OrderingOptions _options;

    public OrderCheckoutService(
        ICartQuery cart,
        IInventoryOperations inventory,
        IOrderRepository orders,
        IIntegrationEventPublisher events,
        IOrderUnitOfWork unitOfWork,
        TimeProvider clock,
        IOptions<OrderingOptions> options)
    {
        _cart = cart;
        _inventory = inventory;
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
        var discountTotal = 0m; // promotions applied in Phase 11
        var taxTotal = Math.Round(cart.Subtotal * (_options.TaxRatePercent / 100m), 2, MidpointRounding.AwayFromZero);

        var shipping = ToShippingAddress(request.ShippingAddress);
        var lines = cart.Lines.Select(l => (l.ProductId, l.VariantId, l.Sku, l.ProductName, l.UnitPrice, l.Quantity));

        var orderResult = Order.Create(GenerateOrderNumber(now), customerId, cart.CartId, cart.Currency,
            shipping, lines, discountTotal, taxTotal, now);
        if (orderResult.IsFailure)
            return orderResult.Error;
        var order = orderResult.Value;

        // Reserve stock before persisting the order; if any line is short, nothing is created.
        var stockLines = cart.Lines.Select(l => new StockRequest(l.Sku, l.Quantity)).ToArray();
        var reserve = await _inventory.ReserveAsync(cart.CartId, stockLines, TimeSpan.FromMinutes(_options.ReservationHoldMinutes), ct);
        if (reserve.IsFailure)
            return reserve.Error;

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

    private static CheckoutResponse ToResponse(Order order) =>
        new(order.Id, order.OrderNumber, order.Status.ToString(), order.Currency, order.GrandTotal);

    private static Domain.ShippingAddress ToShippingAddress(ShippingAddressRequest a) =>
        new(a.RecipientName.Trim(), a.Line1.Trim(), a.Line2?.Trim(), a.City.Trim(), a.Region?.Trim(),
            a.PostalCode.Trim(), a.Country.Trim().ToUpperInvariant());

    private static string GenerateOrderNumber(DateTimeOffset now) =>
        $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
