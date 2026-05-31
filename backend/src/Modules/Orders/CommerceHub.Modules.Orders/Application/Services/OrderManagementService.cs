using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Inventory.Contracts;
using CommerceHub.Modules.Orders.Application.Abstractions;
using CommerceHub.Modules.Orders.Contracts;
using CommerceHub.Modules.Orders.Domain;

namespace CommerceHub.Modules.Orders.Application.Services;

/// <summary>Customer cancellation and admin fulfillment transitions, each validated by the order state machine.</summary>
public sealed class OrderManagementService
{
    private readonly IOrderRepository _orders;
    private readonly IInventoryOperations _inventory;
    private readonly IIntegrationEventPublisher _events;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public OrderManagementService(
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

    /// <summary>Cancel an order. When <paramref name="customerId"/> is set, only that customer's order may be cancelled.</summary>
    public async Task<Result> CancelAsync(Guid orderId, Guid? customerId, string? reason, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null || (customerId is { } cid && order.CustomerId != cid))
            return OrderErrors.NotFound;

        var heldReservation = order.HoldsReservation;

        var result = order.Cancel(reason, _clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        // Release any stock still merely reserved (no-op if it was already committed or released).
        if (heldReservation)
            await _inventory.ReleaseAsync(order.CartId, ct);

        _events.Enqueue(new OrderCancelledIntegrationEvent(order.Id, order.OrderNumber, order.CustomerId, reason), order.Id);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public Task<Result> StartProcessingAsync(Guid orderId, CancellationToken ct = default) =>
        TransitionAsync(orderId, o => o.StartProcessing(_clock.GetUtcNow()), ct);

    public Task<Result> MarkShippedAsync(Guid orderId, CancellationToken ct = default) =>
        TransitionAsync(orderId, o => o.MarkShipped(_clock.GetUtcNow()), ct);

    public Task<Result> MarkDeliveredAsync(Guid orderId, CancellationToken ct = default) =>
        TransitionAsync(orderId, o => o.MarkDelivered(_clock.GetUtcNow()), ct);

    private async Task<Result> TransitionAsync(Guid orderId, Func<Order, Result> transition, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return OrderErrors.NotFound;

        var result = transition(order);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
