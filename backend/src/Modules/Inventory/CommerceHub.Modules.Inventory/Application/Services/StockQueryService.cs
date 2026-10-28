using CommerceHub.Modules.Inventory.Application.Abstractions;
using CommerceHub.Modules.Inventory.Contracts;

namespace CommerceHub.Modules.Inventory.Application.Services;

/// <summary>Implements the public <see cref="IStockQuery"/> over the read store.</summary>
public sealed class StockQueryService : IStockQuery
{
    private readonly IInventoryReadStore _readStore;

    public StockQueryService(IInventoryReadStore readStore) => _readStore = readStore;

    public async Task<int?> GetAvailableAsync(string sku, CancellationToken ct = default)
    {
        var item = await _readStore.GetBySkuAsync(sku.Trim().ToUpperInvariant(), ct);
        return item is null ? null : Math.Max(0, item.Available);
    }
}
