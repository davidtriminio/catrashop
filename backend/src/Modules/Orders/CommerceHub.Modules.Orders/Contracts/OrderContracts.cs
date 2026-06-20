using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Orders.Contracts;

/// <summary>
/// Public, in-process contract the Payments module calls after processing a payment: confirm captures the
/// order (Paid + commit reserved stock) and fail marks it PaymentFailed. Idempotent — a repeated confirm on an
/// already-paid order is a no-op success.
/// </summary>
public interface IOrderWorkflow
{
    /// <summary>What the Payments module needs to charge an order: amount, currency, owner, and whether it awaits payment.</summary>
    Task<PayableOrder?> GetPayableAsync(Guid orderId, CancellationToken ct = default);

    Task<Result> ConfirmPaymentAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> FailPaymentAsync(Guid orderId, string? reason, CancellationToken ct = default);

    /// <summary>Move a paid/fulfilling order through RefundRequested → Refunded after a payment refund. Idempotent.</summary>
    Task<Result> RefundAsync(Guid orderId, CancellationToken ct = default);
}

public sealed record PayableOrder(Guid OrderId, Guid CustomerId, string Currency, decimal Amount, bool IsAwaitingPayment, bool IsPaid);

/// <summary>
/// Public, in-process contract the Shipping module calls to read an order's destination and drive its
/// fulfillment transitions (Processing → Shipped → Delivered), each validated by the order state machine.
/// </summary>
public interface IOrderFulfillment
{
    Task<FulfillableOrder?> GetFulfillmentAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> StartProcessingAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> MarkShippedAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> MarkDeliveredAsync(Guid orderId, CancellationToken ct = default);
}

public sealed record FulfillableOrder(
    Guid OrderId, Guid CustomerId, string Status,
    string RecipientName, string Line1, string? Line2, string City, string? Region, string PostalCode, string Country);

// --- Integration events (published via the outbox) ----------------------------------------------

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, string Currency, decimal GrandTotal, int ItemCount) : IntegrationEvent;

public sealed record OrderPaidIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, decimal GrandTotal, string Currency) : IntegrationEvent;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, string? Reason) : IntegrationEvent;
