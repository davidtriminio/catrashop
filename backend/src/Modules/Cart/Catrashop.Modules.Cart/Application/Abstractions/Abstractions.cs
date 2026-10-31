using Catrashop.BuildingBlocks.Persistence;
using Catrashop.Modules.Cart.Domain;

namespace Catrashop.Modules.Cart.Application.Abstractions;

/// <summary>The Cart module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface ICartUnitOfWork : IUnitOfWork;

/// <summary>Write-side gateway for the <see cref="Domain.Cart"/> aggregate.</summary>
public interface ICartRepository
{
    void Add(Domain.Cart cart);
    Task<Domain.Cart?> GetActiveByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Domain.Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default);
}
