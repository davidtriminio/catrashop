using CommerceHub.Modules.Shipping.Application.Abstractions;
using CommerceHub.Modules.Shipping.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Shipping.Infrastructure.Persistence;

public sealed class ShippingMethodRepository : IShippingMethodRepository
{
    private readonly ShippingDbContext _context;
    public ShippingMethodRepository(ShippingDbContext context) => _context = context;

    public void Add(ShippingMethod method) => _context.ShippingMethods.Add(method);

    public Task<ShippingMethod?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.ShippingMethods.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<ShippingMethod?> GetByCodeAsync(string code, bool activeOnly, CancellationToken ct = default) =>
        _context.ShippingMethods.FirstOrDefaultAsync(m => m.Code == code && (!activeOnly || m.IsActive), ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        _context.ShippingMethods.AnyAsync(m => m.Code == code, ct);

    // Display-only listing (checkout options / admin list); mutations load through GetByIdAsync, so this
    // read skips change tracking.
    public async Task<IReadOnlyList<ShippingMethod>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
        await _context.ShippingMethods.AsNoTracking().Where(m => !activeOnly || m.IsActive).OrderBy(m => m.Cost).ToListAsync(ct);
}

public sealed class ShipmentRepository : IShipmentRepository
{
    private readonly ShippingDbContext _context;
    public ShipmentRepository(ShippingDbContext context) => _context = context;

    public void Add(Shipment shipment) => _context.Shipments.Add(shipment);

    // Events are an owned collection and load with the shipment automatically.
    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Shipments.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default) =>
        _context.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId, ct);

    public Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct = default) =>
        _context.Shipments.FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, ct);

    public Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken ct = default) =>
        _context.Shipments.AnyAsync(s => s.OrderId == orderId, ct);
}
