using Catrashop.Modules.Customers.Domain;
using Xunit;

namespace Catrashop.Modules.Customers.Tests;

public class CustomerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    private static Customer NewCustomer() =>
        Customer.Create(Guid.NewGuid(), "jane@example.com", "Jane", "Doe", Now);

    private static Address Add(Customer c, string label = "Home") =>
        c.AddAddress(label, "Jane Doe", "1 Main St", null, "Springfield", "IL", "62704", "US", null, Now);

    [Fact]
    public void First_address_becomes_default_shipping_and_billing()
    {
        var customer = NewCustomer();
        var address = Add(customer);

        Assert.True(address.IsDefaultShipping);
        Assert.True(address.IsDefaultBilling);
    }

    [Fact]
    public void Second_address_is_not_default_until_set()
    {
        var customer = NewCustomer();
        Add(customer, "Home");
        var work = Add(customer, "Work");

        Assert.False(work.IsDefaultShipping);
        Assert.False(work.IsDefaultBilling);
    }

    [Fact]
    public void Setting_default_shipping_unsets_the_previous_default()
    {
        var customer = NewCustomer();
        var home = Add(customer, "Home");
        var work = Add(customer, "Work");

        var result = customer.SetDefaultShipping(work.Id, Now);

        Assert.True(result.IsSuccess);
        Assert.True(work.IsDefaultShipping);
        Assert.False(home.IsDefaultShipping);
        Assert.True(home.IsDefaultBilling); // billing default unchanged
    }

    [Fact]
    public void Setting_default_for_unknown_address_fails()
    {
        var customer = NewCustomer();
        Add(customer);

        var result = customer.SetDefaultShipping(Guid.NewGuid(), Now);

        Assert.True(result.IsFailure);
        Assert.Equal("customers.address.not_found", result.Error.Code);
    }

    [Fact]
    public void Removing_unknown_address_fails()
    {
        var customer = NewCustomer();

        var result = customer.RemoveAddress(Guid.NewGuid(), Now);

        Assert.True(result.IsFailure);
        Assert.Equal("customers.address.not_found", result.Error.Code);
    }

    [Fact]
    public void Wishlist_does_not_add_duplicates()
    {
        var customer = NewCustomer();
        var productId = Guid.NewGuid();

        customer.AddToWishlist(productId, Now);
        customer.AddToWishlist(productId, Now);

        Assert.Single(customer.WishlistItems);
    }

    [Fact]
    public void Wishlist_remove_takes_the_product_out()
    {
        var customer = NewCustomer();
        var productId = Guid.NewGuid();
        customer.AddToWishlist(productId, Now);

        customer.RemoveFromWishlist(productId, Now);

        Assert.Empty(customer.WishlistItems);
    }

    [Fact]
    public void Customer_id_equals_the_user_id()
    {
        var userId = Guid.NewGuid();
        var customer = Customer.Create(userId, "a@b.com", "A", "B", Now);

        Assert.Equal(userId, customer.Id);
    }
}
