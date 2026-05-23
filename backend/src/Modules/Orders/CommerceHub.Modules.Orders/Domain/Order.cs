using CommerceHub.BuildingBlocks.Domain;
using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Orders.Domain;

/// <summary>
/// The order aggregate root. Created from a cart at checkout with its lines and totals captured, it then moves
/// through the lifecycle only via <see cref="OrderStateMachine"/>-validated transitions, each recorded in the
/// status history. <see cref="CartId"/> doubles as the inventory reservation reference, so payment can later
/// commit or release exactly the stock this order holds.
/// </summary>
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();
    private readonly List<OrderStatusHistory> _statusHistory = new();

    private Order() { } // EF

    private Order(Guid id, string orderNumber, Guid customerId, Guid cartId, string currency,
        ShippingAddress shippingAddress, decimal discountTotal, decimal taxTotal, DateTimeOffset nowUtc) : base(id)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        CartId = cartId;
        Currency = currency;
        ShippingAddress = shippingAddress;
        DiscountTotal = discountTotal;
        TaxTotal = taxTotal;
        Status = OrderStatus.Pending;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string OrderNumber { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    /// <summary>The originating cart id; also the inventory reservation reference for this order.</summary>
    public Guid CartId { get; private set; }

    public string Currency { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = default!;

    public decimal DiscountTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal Subtotal => _items.Sum(i => i.LineTotal);
    public decimal GrandTotal => Subtotal - DiscountTotal + TaxTotal;

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public static Result<Order> Create(
        string orderNumber, Guid customerId, Guid cartId, string currency, ShippingAddress shippingAddress,
        IEnumerable<(Guid ProductId, Guid VariantId, string Sku, string ProductName, decimal UnitPrice, int Quantity)> lines,
        decimal discountTotal, decimal taxTotal, DateTimeOffset nowUtc)
    {
        var order = new Order(Guid.NewGuid(), orderNumber, customerId, cartId, currency.ToUpperInvariant(),
            shippingAddress, discountTotal, taxTotal, nowUtc);

        foreach (var l in lines)
            order._items.Add(new OrderItem(Guid.NewGuid(), order.Id, l.ProductId, l.VariantId, l.Sku, l.ProductName, l.UnitPrice, l.Quantity));

        if (order._items.Count == 0)
            return OrderErrors.NoLines;

        order.RecordHistory(OrderStatus.Pending, "Order created", nowUtc);
        return order;
    }

    // --- Lifecycle transitions (each validated by the state machine) ----------------------------

    public Result MarkPaymentPending(DateTimeOffset nowUtc) => Transition(OrderStatus.PaymentPending, "Payment initiated", nowUtc);
    public Result MarkPaid(DateTimeOffset nowUtc) => Transition(OrderStatus.Paid, "Payment captured", nowUtc);
    public Result MarkPaymentFailed(string? reason, DateTimeOffset nowUtc) => Transition(OrderStatus.PaymentFailed, reason ?? "Payment failed", nowUtc);
    public Result StartProcessing(DateTimeOffset nowUtc) => Transition(OrderStatus.Processing, "Processing", nowUtc);
    public Result MarkShipped(DateTimeOffset nowUtc) => Transition(OrderStatus.Shipped, "Shipped", nowUtc);
    public Result MarkDelivered(DateTimeOffset nowUtc) => Transition(OrderStatus.Delivered, "Delivered", nowUtc);
    public Result Cancel(string? reason, DateTimeOffset nowUtc) => Transition(OrderStatus.Cancelled, reason ?? "Cancelled", nowUtc);
    public Result RequestRefund(string? reason, DateTimeOffset nowUtc) => Transition(OrderStatus.RefundRequested, reason ?? "Refund requested", nowUtc);
    public Result CompleteRefund(DateTimeOffset nowUtc) => Transition(OrderStatus.Refunded, "Refund completed", nowUtc);

    /// <summary>True while the order still holds an inventory reservation (created, awaiting/failed payment).</summary>
    public bool HoldsReservation => Status is OrderStatus.Pending or OrderStatus.PaymentPending or OrderStatus.PaymentFailed;

    private Result Transition(OrderStatus to, string? note, DateTimeOffset nowUtc)
    {
        if (!OrderStateMachine.CanTransition(Status, to))
            return OrderErrors.InvalidTransition(Status, to);

        Status = to;
        RecordHistory(to, note, nowUtc);
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    private void RecordHistory(OrderStatus status, string? note, DateTimeOffset nowUtc) =>
        _statusHistory.Add(new OrderStatusHistory(Guid.NewGuid(), Id, status, note, nowUtc));
}
