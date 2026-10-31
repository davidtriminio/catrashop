using Catrashop.BuildingBlocks.Pagination;
using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Inventory.Application.Abstractions;
using Catrashop.Modules.Inventory.Application.Contracts;
using Catrashop.Modules.Inventory.Domain;

namespace Catrashop.Modules.Inventory.Application.Services;

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
