using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Inventory.Contracts;
using Catrashop.Modules.Orders.Application.Abstractions;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Orders.Domain;
using Catrashop.Modules.Promotions.Contracts;

namespace Catrashop.Modules.Orders.Application.Services;

/// <summary>
/// Implements <see cref="IOrderWorkflow"/> — the callbacks the Payments module invokes. Confirming a payment
/// marks the order Paid and commits the reserved stock; failing leaves the reservation in place so the customer
/// can retry (it expires on its own otherwise). Both are idempotent.
/// </summary>
public sealed class OrderWorkflowService : IOrderWorkflow
{
    private readonly IOrderRepository _orders;
    private readonly IInventoryOperations _inventory;
    private readonly IPromotionApplication _promotions;
    private readonly IIntegrationEventPublisher _events;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public OrderWorkflowService(
        IOrderRepository orders,
        IInventoryOperations inventory,
        IPromotionApplication promotions,
        IIntegrationEventPublisher events,
        IOrderUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _orders = orders;
        _inventory = inventory;
        _promotions = promotions;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<PayableOrder?> GetPayableAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return null;

        var awaiting = order.Status is OrderStatus.Pending or OrderStatus.PaymentPending or OrderStatus.PaymentFailed;
        var paid = order.Status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered;
        return new PayableOrder(order.Id, order.CustomerId, order.Currency, order.GrandTotal, awaiting, paid);
    }

    public async Task<Result> RefundAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return OrderErrors.NotFound;

        if (order.Status == OrderStatus.Refunded)
            return Result.Success(); // idempotent

        var now = _clock.GetUtcNow();
        if (order.Status != OrderStatus.RefundRequested)
        {
            var requested = order.RequestRefund("Payment refunded", now);
            if (requested.IsFailure)
                return requested;
        }

        var completed = order.CompleteRefund(now);
        if (completed.IsFailure)
            return completed;

        // A refunded order returns its coupon redemption (idempotent no-op when none was used).
        await _promotions.ReleaseAsync(order.CartId, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ConfirmPaymentAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return OrderErrors.NotFound;

        if (order.Status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered)
            return Result.Success(); // already captured — idempotent

        var result = order.MarkPaid(_clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        // The reserved stock is now sold: commit it (idempotent by cart id).
        await _inventory.CommitAsync(order.CartId, ct);

        _events.Enqueue(new OrderPaidIntegrationEvent(order.Id, order.OrderNumber, order.CustomerId, order.GrandTotal, order.Currency), order.Id);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> FailPaymentAsync(Guid orderId, string? reason, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return OrderErrors.NotFound;

        if (order.Status == OrderStatus.PaymentFailed)
            return Result.Success(); // idempotent

        var result = order.MarkPaymentFailed(reason, _clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
