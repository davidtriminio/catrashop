using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Inventory.Contracts;

/// <summary>One line of a multi-SKU stock operation.</summary>
public sealed record StockRequest(string Sku, int Quantity);

/// <summary>
/// The Inventory module's public, in-process API for other modules (e.g. Orders during checkout).
/// All operations are idempotent by <c>reference</c> (the order/checkout id) and atomic across lines:
/// a reservation either holds every line or none. This is the only Inventory surface other modules
/// depend on — the domain and persistence stay internal.
/// </summary>
public interface IInventoryOperations
{
    /// <summary>Reserve stock for every line, or fail without reserving any (prevents partial holds).</summary>
    Task<Result> ReserveAsync(Guid reference, IReadOnlyCollection<StockRequest> lines, TimeSpan holdFor, CancellationToken ct = default);

    /// <summary>Release a previously-held reservation (idempotent).</summary>
    Task<Result> ReleaseAsync(Guid reference, CancellationToken ct = default);

    /// <summary>Commit (fulfill) a held reservation: units leave stock (idempotent).</summary>
    Task<Result> CommitAsync(Guid reference, CancellationToken ct = default);
}

/// <summary>Read-only stock lookup for other modules (e.g. Cart validating a quantity against availability).</summary>
public interface IStockQuery
{
    /// <summary>Units that can still be sold for the SKU, or null when Inventory does not track it.</summary>
    Task<int?> GetAvailableAsync(string sku, CancellationToken ct = default);
}

// --- Integration events (published via the outbox) ----------------------------------------------

public sealed record InventoryReservedIntegrationEvent(Guid Reference, string Sku, int Quantity) : IntegrationEvent;

public sealed record InventoryReleasedIntegrationEvent(Guid Reference, string Sku, int Quantity) : IntegrationEvent;

public sealed record LowInventoryDetectedIntegrationEvent(string Sku, int Available, int Threshold) : IntegrationEvent;
