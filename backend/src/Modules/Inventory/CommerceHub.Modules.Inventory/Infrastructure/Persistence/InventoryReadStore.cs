using CommerceHub.BuildingBlocks.Pagination;
using CommerceHub.Modules.Inventory.Application.Abstractions;
using CommerceHub.Modules.Inventory.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Inventory.Infrastructure.Persistence;

/// <summary>Projection-based read model for the admin inventory views (no aggregate loading).</summary>
public sealed class InventoryReadStore : IInventoryReadStore
{
    private readonly InventoryDbContext _context;
    public InventoryReadStore(InventoryDbContext context) => _context = context;

    public async Task<PagedResult<InventoryItemResponse>> SearchAsync(bool lowStockOnly, int page, int pageSize, CancellationToken ct = default)
    {
        var request = new PageRequest(page, pageSize);
        var query = _context.InventoryItems.AsQueryable();

        if (lowStockOnly)
            query = query.Where(i => i.LowStockThreshold > 0 && (i.QuantityOnHand - i.QuantityReserved) <= i.LowStockThreshold);

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderBy(i => i.Sku)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(i => new InventoryItemResponse(
                i.Id, i.Sku, i.QuantityOnHand, i.QuantityReserved, i.QuantityOnHand - i.QuantityReserved,
                i.LowStockThreshold,
                i.LowStockThreshold > 0 && (i.QuantityOnHand - i.QuantityReserved) <= i.LowStockThreshold))
            .ToListAsync(ct);

        return new PagedResult<InventoryItemResponse>(items, request.Page, request.PageSize, total);
    }

    public Task<InventoryItemResponse?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        _context.InventoryItems
            .Where(i => i.Sku == sku)
            .Select(i => new InventoryItemResponse(
                i.Id, i.Sku, i.QuantityOnHand, i.QuantityReserved, i.QuantityOnHand - i.QuantityReserved,
                i.LowStockThreshold,
                i.LowStockThreshold > 0 && (i.QuantityOnHand - i.QuantityReserved) <= i.LowStockThreshold))
            .FirstOrDefaultAsync(ct)!;

    public async Task<PagedResult<StockMovementResponse>> GetMovementsAsync(string sku, int page, int pageSize, CancellationToken ct = default)
    {
        var request = new PageRequest(page, pageSize);

        var movements = _context.InventoryItems
            .Where(i => i.Sku == sku)
            .SelectMany(i => i.Movements);

        var total = await movements.LongCountAsync(ct);

        var rows = await movements
            .OrderByDescending(m => m.OccurredAtUtc)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(m => new { m.Id, m.Type, m.Quantity, m.Reason, m.Reference, m.OccurredAtUtc })
            .ToListAsync(ct);

        var items = rows
            .Select(m => new StockMovementResponse(m.Id, m.Type.ToString(), m.Quantity, m.Reason, m.Reference, m.OccurredAtUtc))
            .ToArray();

        return new PagedResult<StockMovementResponse>(items, request.Page, request.PageSize, total);
    }
}
