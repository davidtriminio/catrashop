using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.BuildingBlocks.Pagination;
using CommerceHub.BuildingBlocks.Persistence;
using CommerceHub.Modules.Inventory.Application.Contracts;
using CommerceHub.Modules.Inventory.Domain;

namespace CommerceHub.Modules.Inventory.Application.Abstractions;

/// <summary>The Inventory module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface IInventoryUnitOfWork : IUnitOfWork;

/// <summary>Enqueues integration events into the Inventory outbox within the current unit of work.</summary>
public interface IIntegrationEventPublisher
{
    void Enqueue(IIntegrationEvent integrationEvent, Guid? correlationId = null);
}

/// <summary>Write-side gateway for the <see cref="InventoryItem"/> aggregate.</summary>
public interface IInventoryItemRepository
{
    void Add(InventoryItem item);
    Task<bool> ExistsAsync(string sku, CancellationToken ct = default);
    Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItem>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken ct = default);

    /// <summary>Loads every item holding a reservation with the given reference (for release/commit).</summary>
    Task<IReadOnlyList<InventoryItem>> GetByReservationReferenceAsync(Guid reference, CancellationToken ct = default);
}

/// <summary>Read-side projections for the admin inventory views.</summary>
public interface IInventoryReadStore
{
    Task<PagedResult<InventoryItemResponse>> SearchAsync(bool lowStockOnly, int page, int pageSize, CancellationToken ct = default);
    Task<InventoryItemResponse?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<PagedResult<StockMovementResponse>> GetMovementsAsync(string sku, int page, int pageSize, CancellationToken ct = default);
}
