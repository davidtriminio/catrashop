using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Customers.Application.Abstractions;
using CommerceHub.Modules.Customers.Application.Contracts;
using CommerceHub.Modules.Customers.Domain;

namespace CommerceHub.Modules.Customers.Application.Services;

/// <summary>Address book operations, always scoped to the calling customer (userId).</summary>
public sealed class AddressService
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public AddressService(ICustomerRepository customers, ICustomerUnitOfWork unitOfWork, TimeProvider clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<AddressResponse>>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(userId, ct);
        if (customer is null)
            return CustomerErrors.NotFound;

        return Result.Success<IReadOnlyList<AddressResponse>>(
            customer.Addresses.Select(CustomerMappers.ToAddress).ToArray());
    }

    public async Task<Result<AddressResponse>> AddAsync(Guid userId, AddressRequest request, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(userId, ct);
        if (customer is null)
            return CustomerErrors.NotFound;

        var address = customer.AddAddress(request.Label, request.RecipientName, request.Line1, request.Line2,
            request.City, request.Region, request.PostalCode, request.Country, request.Phone, _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return CustomerMappers.ToAddress(address);
    }

    public Task<Result> UpdateAsync(Guid userId, Guid addressId, AddressRequest request, CancellationToken ct = default) =>
        MutateAsync(userId, customer => customer.UpdateAddress(addressId, request.Label, request.RecipientName,
            request.Line1, request.Line2, request.City, request.Region, request.PostalCode, request.Country,
            request.Phone, _clock.GetUtcNow()), ct);

    public Task<Result> RemoveAsync(Guid userId, Guid addressId, CancellationToken ct = default) =>
        MutateAsync(userId, customer => customer.RemoveAddress(addressId, _clock.GetUtcNow()), ct);

    public Task<Result> SetDefaultShippingAsync(Guid userId, Guid addressId, CancellationToken ct = default) =>
        MutateAsync(userId, customer => customer.SetDefaultShipping(addressId, _clock.GetUtcNow()), ct);

    public Task<Result> SetDefaultBillingAsync(Guid userId, Guid addressId, CancellationToken ct = default) =>
        MutateAsync(userId, customer => customer.SetDefaultBilling(addressId, _clock.GetUtcNow()), ct);

    private async Task<Result> MutateAsync(Guid userId, Func<Customer, Result> mutate, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(userId, ct);
        if (customer is null)
            return CustomerErrors.NotFound;

        var result = mutate(customer);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
