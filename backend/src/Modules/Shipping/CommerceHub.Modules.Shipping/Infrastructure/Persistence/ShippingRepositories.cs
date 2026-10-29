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

public sealed class ShippingZoneRepository : IShippingZoneRepository
{
    private readonly ShippingDbContext _context;
    public ShippingZoneRepository(ShippingDbContext context) => _context = context;

    public void Add(ShippingZone zone) => _context.ShippingZones.Add(zone);

    // Departments are an owned collection and load with the zone.
    public Task<ShippingZone?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.ShippingZones.FirstOrDefaultAsync(z => z.Id == id, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        _context.ShippingZones.AnyAsync(z => z.Code == code, ct);

    public async Task<IReadOnlyList<ShippingZone>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
        await _context.ShippingZones.AsNoTracking().Where(z => !activeOnly || z.IsActive).OrderBy(z => z.Cost).ThenBy(z => z.Name).ToListAsync(ct);

    public Task<ShippingZone?> GetActiveByDepartmentAsync(string departmentCode, CancellationToken ct = default) =>
        _context.ShippingZones.AsNoTracking()
            .FirstOrDefaultAsync(z => z.IsActive && z.Departments.Any(d => d.DepartmentCode == departmentCode), ct);

    public async Task<IReadOnlyList<string>> DepartmentsInOtherZonesAsync(
        IEnumerable<string> departmentCodes, Guid? exceptZoneId, CancellationToken ct = default)
    {
        var codes = departmentCodes.ToArray();
        return await _context.ShippingZones
            .Where(z => exceptZoneId == null || z.Id != exceptZoneId)
            .SelectMany(z => z.Departments)
            .Where(d => codes.Contains(d.DepartmentCode))
            .Select(d => d.DepartmentCode)
            .ToListAsync(ct);
    }
}

public sealed class PickupStoreRepository : IPickupStoreRepository
{
    private readonly ShippingDbContext _context;
    public PickupStoreRepository(ShippingDbContext context) => _context = context;

    public void Add(PickupStore store) => _context.PickupStores.Add(store);

    public Task<PickupStore?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.PickupStores.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<PickupStore>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
        await _context.PickupStores.AsNoTracking().Where(s => !activeOnly || s.IsActive).OrderBy(s => s.Name).ToListAsync(ct);
}
