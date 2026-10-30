using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Shipping.Application.Abstractions;
using Catrashop.Modules.Shipping.Application.Contracts;
using Catrashop.Modules.Shipping.Domain;

namespace Catrashop.Modules.Shipping.Application.Services;

/// <summary>
/// Admin management and public listing of shipping zones, plus the checkout-time quotation the Orders module
/// consumes through <see cref="IShippingQuotation"/>: the delivery cost depends on the destination zone only.
/// </summary>
public sealed class ShippingZoneService : IShippingQuotation
{
    private readonly IShippingZoneRepository _zones;
    private readonly IPickupStoreRepository _stores;
    private readonly IShippingUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public ShippingZoneService(IShippingZoneRepository zones, IPickupStoreRepository stores, IShippingUnitOfWork unitOfWork, TimeProvider clock)
    {
        _zones = zones;
        _stores = stores;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    // --- Zones (admin) ---------------------------------------------------------------------------

    public async Task<Result<ShippingZoneResponse>> CreateAsync(CreateShippingZoneRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (await _zones.CodeExistsAsync(code, ct))
            return ShippingErrors.ZoneCodeInUse(code);

        var departments = Normalize(request.DepartmentCodes);
        var taken = await _zones.DepartmentsInOtherZonesAsync(departments, null, ct);
        if (taken.Count > 0)
            return ShippingErrors.DepartmentsInUse(taken);

        var zone = ShippingZone.Create(code, request.Name, request.Cost, request.Currency, request.EstimatedDaysMin,
            request.EstimatedDaysMax, request.CashOnDeliveryCovered, departments, _clock.GetUtcNow());
        _zones.Add(zone);
        await _unitOfWork.SaveChangesAsync(ct);
        return ShippingMappers.ToResponse(zone);
    }

    public async Task<Result<ShippingZoneResponse>> UpdateAsync(Guid id, UpdateShippingZoneRequest request, CancellationToken ct = default)
    {
        var zone = await _zones.GetByIdAsync(id, ct);
        if (zone is null)
            return ShippingErrors.ZoneNotFound;

        var departments = Normalize(request.DepartmentCodes);
        var taken = await _zones.DepartmentsInOtherZonesAsync(departments, id, ct);
        if (taken.Count > 0)
            return ShippingErrors.DepartmentsInUse(taken);

        zone.Update(request.Name, request.Cost, request.Currency, request.EstimatedDaysMin, request.EstimatedDaysMax,
            request.CashOnDeliveryCovered, departments, _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return ShippingMappers.ToResponse(zone);
    }

    public async Task<Result> SetZoneActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        var zone = await _zones.GetByIdAsync(id, ct);
        if (zone is null)
            return ShippingErrors.ZoneNotFound;

        if (active) zone.Activate(_clock.GetUtcNow());
        else zone.Deactivate(_clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<ShippingZoneResponse>> ListZonesAsync(bool activeOnly, CancellationToken ct = default) =>
        (await _zones.ListAsync(activeOnly, ct)).Select(ShippingMappers.ToResponse).ToArray();

    // --- Pickup stores (admin) ---------------------------------------------------------------------

    public async Task<Result<PickupStoreResponse>> CreateStoreAsync(SavePickupStoreRequest request, CancellationToken ct = default)
    {
        var store = PickupStore.Create(request.Name, request.Line1, request.City,
            HondurasDepartments.Normalize(request.DepartmentCode) ?? request.DepartmentCode, request.Phone, request.Hours, _clock.GetUtcNow());
        _stores.Add(store);
        await _unitOfWork.SaveChangesAsync(ct);
        return ShippingMappers.ToResponse(store);
    }

    public async Task<Result<PickupStoreResponse>> UpdateStoreAsync(Guid id, SavePickupStoreRequest request, CancellationToken ct = default)
    {
        var store = await _stores.GetByIdAsync(id, ct);
        if (store is null)
            return ShippingErrors.PickupStoreNotFound;

        store.Update(request.Name, request.Line1, request.City,
            HondurasDepartments.Normalize(request.DepartmentCode) ?? request.DepartmentCode, request.Phone, request.Hours, _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return ShippingMappers.ToResponse(store);
    }

    public async Task<Result> SetStoreActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        var store = await _stores.GetByIdAsync(id, ct);
        if (store is null)
            return ShippingErrors.PickupStoreNotFound;

        if (active) store.Activate(_clock.GetUtcNow());
        else store.Deactivate(_clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<PickupStoreResponse>> ListStoresAsync(bool activeOnly, CancellationToken ct = default) =>
        (await _stores.ListAsync(activeOnly, ct)).Select(ShippingMappers.ToResponse).ToArray();

    // --- IShippingQuotation (used by Orders at checkout) -------------------------------------------

    public async Task<DeliveryQuote?> QuoteDeliveryAsync(string departmentCode, CancellationToken ct = default)
    {
        var department = HondurasDepartments.Find(departmentCode);
        if (department is null)
            return null;

        var zone = await _zones.GetActiveByDepartmentAsync(department.Code, ct);
        return zone is null
            ? null
            : new DeliveryQuote(department.Code, department.Name, zone.Code, zone.Cost, zone.Currency,
                zone.EstimatedDaysMin, zone.EstimatedDaysMax, zone.CashOnDeliveryCovered);
    }

    public async Task<PickupStoreInfo?> GetPickupStoreAsync(Guid storeId, CancellationToken ct = default)
    {
        var store = await _stores.GetByIdAsync(storeId, ct);
        if (store is null || !store.IsActive)
            return null;

        var name = HondurasDepartments.Find(store.DepartmentCode)?.Name ?? store.DepartmentCode;
        return new PickupStoreInfo(store.Id, store.Name, store.Line1, store.City, store.DepartmentCode, name);
    }

    private static string[] Normalize(IEnumerable<string> codes) =>
        codes.Select(c => HondurasDepartments.Normalize(c) ?? c.Trim().ToUpperInvariant()).Distinct().ToArray();
}
