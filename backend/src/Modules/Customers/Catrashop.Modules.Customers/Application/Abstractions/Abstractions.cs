using Catrashop.BuildingBlocks.Persistence;
using Catrashop.Modules.Customers.Domain;

namespace Catrashop.Modules.Customers.Application.Abstractions;

/// <summary>The Customers module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface ICustomerUnitOfWork : IUnitOfWork;

/// <summary>Write-side gateway for the <see cref="Customer"/> aggregate (id = user id).</summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Persists a new customer. Returns <c>false</c> (and persists nothing) when a customer with the same id already
    /// exists — including when another request inserted it concurrently — so provisioning is safely idempotent.
    /// </summary>
    Task<bool> TryAddAsync(Customer customer, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a customer with its addresses and wishlist.</summary>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
