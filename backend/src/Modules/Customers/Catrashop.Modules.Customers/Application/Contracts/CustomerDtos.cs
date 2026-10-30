namespace Catrashop.Modules.Customers.Application.Contracts;

// --- Requests -----------------------------------------------------------------------------------

public sealed record UpdateProfileRequest(string FirstName, string LastName, string? Phone);

public sealed record PreferencesRequest(bool MarketingEmailsOptIn, string PreferredLanguage);

public sealed record AddressRequest(
    string Label,
    string RecipientName,
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string PostalCode,
    string Country,
    string? Phone);

public sealed record AddWishlistItemRequest(Guid ProductId);

// --- Responses ----------------------------------------------------------------------------------

public sealed record CustomerProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    bool MarketingEmailsOptIn,
    string PreferredLanguage);

public sealed record AddressResponse(
    Guid Id,
    string Label,
    string RecipientName,
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string PostalCode,
    string Country,
    string? Phone,
    bool IsDefaultShipping,
    bool IsDefaultBilling);

public sealed record WishlistItemResponse(Guid ProductId, DateTimeOffset AddedAtUtc);

public sealed record CreatedResource(Guid Id);
