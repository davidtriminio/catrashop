using Catrashop.Modules.Customers.Application.Contracts;
using Catrashop.Modules.Customers.Domain;

namespace Catrashop.Modules.Customers.Application.Services;

internal static class CustomerMappers
{
    public static CustomerProfileResponse ToProfile(Customer c) => new(
        c.Id, c.Email, c.FirstName, c.LastName, c.Phone, c.MarketingEmailsOptIn, c.PreferredLanguage);

    public static AddressResponse ToAddress(Address a) => new(
        a.Id, a.Label, a.RecipientName, a.Line1, a.Line2, a.City, a.Region, a.PostalCode, a.Country, a.Phone,
        a.IsDefaultShipping, a.IsDefaultBilling);

    public static WishlistItemResponse ToWishlistItem(WishlistItem w) => new(w.ProductId, w.AddedAtUtc);
}
