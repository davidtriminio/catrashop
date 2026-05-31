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
    Task<Result> ConfirmPaymentAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> FailPaymentAsync(Guid orderId, string? reason, CancellationToken ct = default);
}

// --- Integration events (published via the outbox) ----------------------------------------------

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, string Currency, decimal GrandTotal, int ItemCount) : IntegrationEvent;

public sealed record OrderPaidIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, decimal GrandTotal, string Currency) : IntegrationEvent;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, string? Reason) : IntegrationEvent;
