using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.BuildingBlocks.Persistence;
using CommerceHub.Modules.Shipping.Domain;

namespace CommerceHub.Modules.Shipping.Application.Abstractions;

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

public interface IShipmentRepository
{
    void Add(Shipment shipment);
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct = default);
    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken ct = default);
}
