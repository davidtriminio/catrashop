using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Customers.Application.Abstractions;
using Catrashop.Modules.Customers.Application.Contracts;
using Catrashop.Modules.Customers.Domain;

namespace Catrashop.Modules.Customers.Application.Services;

/// <summary>Wishlist operations, scoped to the calling customer.</summary>
public sealed class WishlistService
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public WishlistService(ICustomerRepository customers, ICustomerUnitOfWork unitOfWork, TimeProvider clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<WishlistItemResponse>>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(userId, ct);
        if (customer is null)
            return CustomerErrors.NotFound;

        return Result.Success<IReadOnlyList<WishlistItemResponse>>(
            customer.WishlistItems.OrderByDescending(w => w.AddedAtUtc).Select(CustomerMappers.ToWishlistItem).ToArray());
    }

    public async Task<Result> AddAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(userId, ct);
        if (customer is null)
            return CustomerErrors.NotFound;

        customer.AddToWishlist(productId, _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(userId, ct);
        if (customer is null)
            return CustomerErrors.NotFound;

        customer.RemoveFromWishlist(productId, _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
