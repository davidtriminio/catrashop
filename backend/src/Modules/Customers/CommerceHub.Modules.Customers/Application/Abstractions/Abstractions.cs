using CommerceHub.BuildingBlocks.Persistence;
using CommerceHub.Modules.Customers.Domain;

namespace CommerceHub.Modules.Customers.Application.Abstractions;

/// <summary>The Customers module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface ICustomerUnitOfWork : IUnitOfWork;

/// <summary>Write-side gateway for the <see cref="Customer"/> aggregate (id = user id).</summary>
public interface ICustomerRepository
{
    void Add(Customer customer);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a customer with its addresses and wishlist.</summary>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
