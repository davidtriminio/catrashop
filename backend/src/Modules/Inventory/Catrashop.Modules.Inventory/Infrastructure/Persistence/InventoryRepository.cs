using Catrashop.Modules.Inventory.Application.Abstractions;
using Catrashop.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryItemRepository : IInventoryItemRepository
{
    private readonly InventoryDbContext _context;
    public InventoryItemRepository(InventoryDbContext context) => _context = context;

    public void Add(InventoryItem item) => _context.InventoryItems.Add(item);

    public Task<bool> ExistsAsync(string sku, CancellationToken ct = default) =>
        _context.InventoryItems.AnyAsync(i => i.Sku == sku, ct);

    public Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        _context.InventoryItems.FirstOrDefaultAsync(i => i.Sku == sku, ct);

    public async Task<IReadOnlyList<InventoryItem>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken ct = default) =>
        await _context.InventoryItems.Where(i => skus.Contains(i.Sku)).ToListAsync(ct);

    public async Task<IReadOnlyList<InventoryItem>> GetByReservationReferenceAsync(Guid reference, CancellationToken ct = default) =>
        await _context.InventoryItems.Where(i => i.Reservations.Any(r => r.Reference == reference)).ToListAsync(ct);
}
