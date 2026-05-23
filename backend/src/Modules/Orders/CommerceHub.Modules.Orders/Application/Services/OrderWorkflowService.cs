using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Inventory.Contracts;
using CommerceHub.Modules.Orders.Application.Abstractions;
using CommerceHub.Modules.Orders.Contracts;
using CommerceHub.Modules.Orders.Domain;

namespace CommerceHub.Modules.Orders.Application.Services;

/// <summary>
/// Implements <see cref="IOrderWorkflow"/> — the callbacks the Payments module invokes. Confirming a payment
/// marks the order Paid and commits the reserved stock; failing leaves the reservation in place so the customer
/// can retry (it expires on its own otherwise). Both are idempotent.
/// </summary>
public sealed class OrderWorkflowService : IOrderWorkflow
{
    private readonly IOrderRepository _orders;
    private readonly IInventoryOperations _inventory;
    private readonly IIntegrationEventPublisher _events;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public OrderWorkflowService(
        IOrderRepository orders,
        IInventoryOperations inventory,
        IIntegrationEventPublisher events,
        IOrderUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _orders = orders;
        _inventory = inventory;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
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
