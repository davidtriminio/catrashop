using Catrashop.BuildingBlocks.Messaging;
using Catrashop.BuildingBlocks.Persistence;
using Catrashop.Modules.Shipping.Domain;

namespace Catrashop.Modules.Shipping.Application.Abstractions;

/// <summary>The Shipping module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface IShippingUnitOfWork : IUnitOfWork;

/// <summary>Enqueues integration events into the Shipping outbox within the current unit of work.</summary>
public interface IIntegrationEventPublisher
{
    void Enqueue(IIntegrationEvent integrationEvent, Guid? correlationId = null);
}

public interface IShippingMethodRepository
{
    void Add(ShippingMethod method);
    Task<ShippingMethod?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ShippingMethod?> GetByCodeAsync(string code, bool activeOnly, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<ShippingMethod>> ListAsync(bool activeOnly, CancellationToken ct = default);
}

public interface IShippingZoneRepository
{
    void Add(ShippingZone zone);
    Task<ShippingZone?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<ShippingZone>> ListAsync(bool activeOnly, CancellationToken ct = default);

    /// <summary>The active zone that serves a department, or null when none does.</summary>
    Task<ShippingZone?> GetActiveByDepartmentAsync(string departmentCode, CancellationToken ct = default);

    /// <summary>Of these department codes, the ones already assigned to a zone other than <paramref name="exceptZoneId"/>.</summary>
    Task<IReadOnlyList<string>> DepartmentsInOtherZonesAsync(IEnumerable<string> departmentCodes, Guid? exceptZoneId, CancellationToken ct = default);
}

public interface IPickupStoreRepository
{
    void Add(PickupStore store);
    Task<PickupStore?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PickupStore>> ListAsync(bool activeOnly, CancellationToken ct = default);
}

public interface IShipmentRepository
{
    void Add(Shipment shipment);
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct = default);
    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken ct = default);
}
