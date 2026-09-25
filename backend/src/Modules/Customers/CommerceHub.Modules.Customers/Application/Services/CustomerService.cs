using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Customers.Application.Abstractions;
using CommerceHub.Modules.Customers.Application.Contracts;
using CommerceHub.Modules.Customers.Contracts;
using CommerceHub.Modules.Customers.Domain;

namespace CommerceHub.Modules.Customers.Application.Services;

/// <summary>Customer profile lifecycle: provisioning (get-or-create), read, update, preferences.</summary>
public sealed class CustomerService : ICustomerProvisioning
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public CustomerService(ICustomerRepository customers, ICustomerUnitOfWork unitOfWork, TimeProvider clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task EnsureCustomerAsync(Guid userId, string email, string firstName, string lastName, CancellationToken ct = default)
    {
        if (await _customers.ExistsAsync(userId, ct))
            return; // idempotent

        _customers.Add(Customer.Create(userId, email, firstName, lastName, _clock.GetUtcNow()));
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<Result<CustomerProfileResponse>> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(userId, ct);
        return customer is null ? CustomerErrors.NotFound : CustomerMappers.ToProfile(customer);
    }

    public async Task<Result<CustomerProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(userId, ct);
        if (customer is null)
            return CustomerErrors.NotFound;

        customer.UpdateProfile(request.FirstName, request.LastName, request.Phone, _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return CustomerMappers.ToProfile(customer);
    }

    public async Task<Result<CustomerProfileResponse>> SetPreferencesAsync(Guid userId, PreferencesRequest request, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(userId, ct);
        if (customer is null)
            return CustomerErrors.NotFound;

        customer.SetPreferences(request.MarketingEmailsOptIn, request.PreferredLanguage, _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return CustomerMappers.ToProfile(customer);
    }
}
