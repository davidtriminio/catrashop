using CommerceHub.BuildingBlocks.Pagination;
using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Inventory.Application.Abstractions;
using CommerceHub.Modules.Inventory.Application.Contracts;
using CommerceHub.Modules.Inventory.Domain;

namespace CommerceHub.Modules.Inventory.Application.Services;

/// <summary>Admin read queries over inventory items and their movement history.</summary>
public sealed class InventoryQueryService
{
    private readonly IInventoryReadStore _readStore;

    public InventoryQueryService(IInventoryReadStore readStore) => _readStore = readStore;

    public Task<PagedResult<InventoryItemResponse>> SearchAsync(bool lowStockOnly, int page, int pageSize, CancellationToken ct = default) =>
        _readStore.SearchAsync(lowStockOnly, page, pageSize, ct);

    public async Task<Result<InventoryItemResponse>> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        var item = await _readStore.GetBySkuAsync(sku.Trim().ToUpperInvariant(), ct);
        return item is null ? InventoryErrors.ItemNotFound(sku) : item;
    }

    public Task<PagedResult<StockMovementResponse>> GetMovementsAsync(string sku, int page, int pageSize, CancellationToken ct = default) =>
        _readStore.GetMovementsAsync(sku.Trim().ToUpperInvariant(), page, pageSize, ct);
}
