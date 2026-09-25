namespace CommerceHub.Modules.Customers.Contracts;

/// <summary>
/// Public, in-process contract to ensure a customer profile exists for a user. Called on-demand by the
/// customer endpoints (with the JWT email) and, once messaging lands (Phase 15), by the consumer of the
/// Identity <c>CustomerRegistered</c> event (with the full name). Creating is idempotent: an existing
/// profile is left untouched.
/// </summary>
public interface ICustomerProvisioning
{
    Task EnsureCustomerAsync(Guid userId, string email, string firstName, string lastName, CancellationToken ct = default);
}
