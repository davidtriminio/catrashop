using CommerceHub.BuildingBlocks.Domain;
using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Customers.Domain;

/// <summary>
/// A customer profile. Its <see cref="Entity{TId}.Id"/> is the owning user's id (1:1 with Identity), so a
/// customer can only ever load and mutate their own aggregate — IDOR is impossible by construction. Owns the
/// customer's addresses and wishlist and guards their invariants (single default shipping/billing, no duplicate
/// wishlist entries).
/// </summary>
public sealed class Customer : AggregateRoot<Guid>
{
    private readonly List<Address> _addresses = new();
    private readonly List<WishlistItem> _wishlistItems = new();

    private Customer() { } // EF

    private Customer(Guid userId, string email, string firstName, string lastName, DateTimeOffset nowUtc) : base(userId)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PreferredLanguage = "en";
        MarketingEmailsOptIn = false;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string Email { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? Phone { get; private set; }
    public bool MarketingEmailsOptIn { get; private set; }
    public string PreferredLanguage { get; private set; } = "en";
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<WishlistItem> WishlistItems => _wishlistItems.AsReadOnly();

    public static Customer Create(Guid userId, string email, string firstName, string lastName, DateTimeOffset nowUtc) =>
        new(userId, email.Trim().ToLowerInvariant(), firstName.Trim(), lastName.Trim(), nowUtc);

    public void UpdateProfile(string firstName, string lastName, string? phone, DateTimeOffset nowUtc)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Touch(nowUtc);
    }

    public void SetPreferences(bool marketingEmailsOptIn, string preferredLanguage, DateTimeOffset nowUtc)
    {
        MarketingEmailsOptIn = marketingEmailsOptIn;
        PreferredLanguage = string.IsNullOrWhiteSpace(preferredLanguage) ? "en" : preferredLanguage.Trim();
        Touch(nowUtc);
    }

    // --- Addresses ------------------------------------------------------------------------------

    public Address AddAddress(string label, string recipientName, string line1, string? line2, string city,
        string? region, string postalCode, string country, string? phone, DateTimeOffset nowUtc)
    {
        var address = new Address(Guid.NewGuid(), Id, label.Trim(), recipientName.Trim(), line1.Trim(),
            line2?.Trim(), city.Trim(), region?.Trim(), postalCode.Trim(), country.Trim(), phone?.Trim());

        // The first address becomes the default for both shipping and billing.
        if (_addresses.Count == 0)
        {
            address.SetDefaultShipping(true);
            address.SetDefaultBilling(true);
        }

        _addresses.Add(address);
        Touch(nowUtc);
        return address;
    }

    public Result UpdateAddress(Guid addressId, string label, string recipientName, string line1, string? line2,
        string city, string? region, string postalCode, string country, string? phone, DateTimeOffset nowUtc)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null)
            return CustomerErrors.AddressNotFound;

        address.Update(label.Trim(), recipientName.Trim(), line1.Trim(), line2?.Trim(), city.Trim(),
            region?.Trim(), postalCode.Trim(), country.Trim(), phone?.Trim());
        Touch(nowUtc);
        return Result.Success();
    }

    public Result RemoveAddress(Guid addressId, DateTimeOffset nowUtc)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null)
            return CustomerErrors.AddressNotFound;

        _addresses.Remove(address);
        Touch(nowUtc);
        return Result.Success();
    }

    public Result SetDefaultShipping(Guid addressId, DateTimeOffset nowUtc)
    {
        var target = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (target is null)
            return CustomerErrors.AddressNotFound;

        foreach (var address in _addresses)
            address.SetDefaultShipping(address.Id == addressId);
        Touch(nowUtc);
        return Result.Success();
    }

    public Result SetDefaultBilling(Guid addressId, DateTimeOffset nowUtc)
    {
        var target = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (target is null)
            return CustomerErrors.AddressNotFound;

        foreach (var address in _addresses)
            address.SetDefaultBilling(address.Id == addressId);
        Touch(nowUtc);
        return Result.Success();
    }

    // --- Wishlist -------------------------------------------------------------------------------

    public void AddToWishlist(Guid productId, DateTimeOffset nowUtc)
    {
        if (_wishlistItems.Any(w => w.ProductId == productId))
            return; // already saved; no duplicates

        _wishlistItems.Add(new WishlistItem(Guid.NewGuid(), Id, productId, nowUtc));
        Touch(nowUtc);
    }

    public void RemoveFromWishlist(Guid productId, DateTimeOffset nowUtc)
    {
        var item = _wishlistItems.FirstOrDefault(w => w.ProductId == productId);
        if (item is null)
            return;

        _wishlistItems.Remove(item);
        Touch(nowUtc);
    }

    private void Touch(DateTimeOffset nowUtc) => UpdatedAtUtc = nowUtc;
}
