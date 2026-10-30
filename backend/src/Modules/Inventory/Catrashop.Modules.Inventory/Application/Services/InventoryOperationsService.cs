using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Inventory.Application.Abstractions;
using Catrashop.Modules.Inventory.Contracts;
using Catrashop.Modules.Inventory.Domain;

namespace Catrashop.Modules.Inventory.Application.Services;

/// <summary>
/// Implements the public <see cref="IInventoryOperations"/> contract used by Orders during checkout.
/// Reservation is atomic across lines: if any line can't be satisfied, nothing is persisted (the unit
/// of work is never saved), so there are no partial holds. All operations are idempotent by reference.
/// </summary>
public sealed class InventoryOperationsService : IInventoryOperations
{
    private readonly IInventoryItemRepository _items;
    private readonly IIntegrationEventPublisher _events;
    private readonly IInventoryUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public InventoryOperationsService(
        IInventoryItemRepository items,
        IIntegrationEventPublisher events,
        IInventoryUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _items = items;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> ReserveAsync(Guid reference, IReadOnlyCollection<StockRequest> lines, TimeSpan holdFor, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var expiry = now.Add(holdFor);

        var skus = lines.Select(l => l.Sku.Trim().ToUpperInvariant()).Distinct().ToArray();
        var items = (await _items.GetBySkusAsync(skus, ct)).ToDictionary(i => i.Sku);

        // Every SKU must be tracked before we hold any stock.
        foreach (var sku in skus)
            if (!items.ContainsKey(sku))
                return InventoryErrors.ItemNotFound(sku);

        // Attempt to reserve every line; the first failure aborts without saving (atomic, no partial holds).
        foreach (var line in lines)
        {
            var item = items[line.Sku.Trim().ToUpperInvariant()];
            var result = item.Reserve(reference, line.Quantity, now, expiry);
            if (result.IsFailure)
                return result;
        }

        foreach (var line in lines)
        {
            var item = items[line.Sku.Trim().ToUpperInvariant()];
            _events.Enqueue(new InventoryReservedIntegrationEvent(reference, item.Sku, line.Quantity), reference);
            if (item.ShouldNotifyLowStock())
                _events.Enqueue(new LowInventoryDetectedIntegrationEvent(item.Sku, item.AvailableQuantity, item.LowStockThreshold));
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ReleaseAsync(Guid reference, CancellationToken ct = default)
    {
        var items = await _items.GetByReservationReferenceAsync(reference, ct);
        foreach (var item in items)
        {
            var reserved = item.Reservations.FirstOrDefault(r => r.Reference == reference && r.IsHeld);
            var result = item.Release(reference, _clock.GetUtcNow());
            if (result.IsSuccess && reserved is not null)
                _events.Enqueue(new InventoryReleasedIntegrationEvent(reference, item.Sku, reserved.Quantity), reference);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CommitAsync(Guid reference, CancellationToken ct = default)
    {
        var items = await _items.GetByReservationReferenceAsync(reference, ct);
        foreach (var item in items)
            item.Commit(reference, _clock.GetUtcNow());

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
