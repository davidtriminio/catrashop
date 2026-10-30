namespace Catrashop.Modules.Orders.Domain;

/// <summary>
/// States in the order lifecycle. Transitions between them are constrained by
/// <see cref="OrderStateMachine"/>; an order can never jump arbitrarily (e.g. a Paid order can't return to
/// Pending, an unpaid order can't ship). See ADR-013.
/// </summary>
public enum OrderStatus
{
    Pending = 0,          // created, before payment is initiated
    PaymentPending = 1,   // awaiting payment authorization/capture
    Paid = 2,             // payment captured
    Processing = 3,       // being prepared for fulfillment
    Shipped = 4,          // handed to carrier
    Delivered = 5,        // received by customer
    PaymentFailed = 6,    // payment attempt failed
    Cancelled = 7,        // terminal — cancelled before fulfillment
    RefundRequested = 8,  // a refund has been requested
    Refunded = 9          // terminal — refund completed
}

/// <summary>The allowed transitions between order states, and helpers to query them.</summary>
public static class OrderStateMachine
{
    private static readonly IReadOnlyDictionary<OrderStatus, IReadOnlySet<OrderStatus>> Allowed =
        new Dictionary<OrderStatus, IReadOnlySet<OrderStatus>>
        {
            [OrderStatus.Pending] = Set(OrderStatus.PaymentPending, OrderStatus.Cancelled),
            [OrderStatus.PaymentPending] = Set(OrderStatus.Paid, OrderStatus.PaymentFailed, OrderStatus.Cancelled),
            [OrderStatus.PaymentFailed] = Set(OrderStatus.PaymentPending, OrderStatus.Cancelled),
            [OrderStatus.Paid] = Set(OrderStatus.Processing, OrderStatus.RefundRequested, OrderStatus.Cancelled),
            [OrderStatus.Processing] = Set(OrderStatus.Shipped, OrderStatus.RefundRequested, OrderStatus.Cancelled),
            [OrderStatus.Shipped] = Set(OrderStatus.Delivered, OrderStatus.RefundRequested),
            [OrderStatus.Delivered] = Set(OrderStatus.RefundRequested),
            [OrderStatus.RefundRequested] = Set(OrderStatus.Refunded),
            [OrderStatus.Cancelled] = Set(),
            [OrderStatus.Refunded] = Set()
        };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsTerminal(OrderStatus status) => Allowed[status].Count == 0;

    private static IReadOnlySet<OrderStatus> Set(params OrderStatus[] statuses) => new HashSet<OrderStatus>(statuses);
}
