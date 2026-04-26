using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Inventory.Application.Abstractions;
using CommerceHub.Modules.Inventory.Application.Contracts;
using CommerceHub.Modules.Inventory.Contracts;
using CommerceHub.Modules.Inventory.Domain;

namespace CommerceHub.Modules.Inventory.Application.Services;

/// <summary>Administrative stock operations: create tracking, receive, adjust, set thresholds.</summary>
public sealed class InventoryAdminService
{
    private readonly IInventoryItemRepository _items;
    private readonly IIntegrationEventPublisher _events;
    private readonly IInventoryUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public InventoryAdminService(
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

    public async Task<Result<InventoryItemResponse>> CreateItemAsync(CreateInventoryItemRequest request, CancellationToken ct = default)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await _items.ExistsAsync(sku, ct))
            return InventoryErrors.SkuAlreadyTracked(sku);

        var item = InventoryItem.Create(sku, request.InitialOnHand, request.LowStockThreshold, _clock.GetUtcNow());
        _items.Add(item);
        PublishLowStockIfNeeded(item);

        await _unitOfWork.SaveChangesAsync(ct);
        return Map(item);
    }

    public Task<Result<InventoryItemResponse>> ReceiveAsync(string sku, ReceiveStockRequest request, CancellationToken ct = default) =>
        MutateAsync(sku, item => item.Receive(request.Quantity, request.Reason, _clock.GetUtcNow()), ct);

    public Task<Result<InventoryItemResponse>> AdjustAsync(string sku, AdjustStockRequest request, CancellationToken ct = default) =>
        MutateAsync(sku, item => item.Adjust(request.Delta, request.Reason, _clock.GetUtcNow()), ct);

    public Task<Result<InventoryItemResponse>> SetThresholdAsync(string sku, SetThresholdRequest request, CancellationToken ct = default) =>
        MutateAsync(sku, item =>
        {
            item.SetLowStockThreshold(request.Threshold, _clock.GetUtcNow());
            return Result.Success();
        }, ct);

    private async Task<Result<InventoryItemResponse>> MutateAsync(string sku, Func<InventoryItem, Result> mutate, CancellationToken ct)
    {
        var item = await _items.GetBySkuAsync(sku.Trim().ToUpperInvariant(), ct);
        if (item is null)
            return InventoryErrors.ItemNotFound(sku);

        var result = mutate(item);
        if (result.IsFailure)
            return result.Error;

        PublishLowStockIfNeeded(item);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(item);
    }

    private void PublishLowStockIfNeeded(InventoryItem item)
    {
        if (item.ShouldNotifyLowStock())
            _events.Enqueue(new LowInventoryDetectedIntegrationEvent(item.Sku, item.AvailableQuantity, item.LowStockThreshold));
    }

    internal static InventoryItemResponse Map(InventoryItem item) => new(
        item.Id, item.Sku, item.QuantityOnHand, item.QuantityReserved, item.AvailableQuantity,
        item.LowStockThreshold, item.LowStockThreshold > 0 && item.AvailableQuantity <= item.LowStockThreshold);
}
