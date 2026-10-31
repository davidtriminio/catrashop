using Catrashop.Modules.Cart.Domain;
using Xunit;

namespace Catrashop.Modules.Cart.Tests;

public class CartTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);

    private static Catrashop.Modules.Cart.Domain.Cart NewCart() =>
        Catrashop.Modules.Cart.Domain.Cart.Create(Guid.NewGuid(), Now);

    private static (Guid product, Guid variant) Ids() => (Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void First_item_sets_the_cart_currency_and_subtotal()
    {
        var cart = NewCart();
        var (p, v) = Ids();

        var result = cart.AddItem(p, v, "SKU-1", "Widget", 9.99m, "USD", 2, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", cart.Currency);
        Assert.Equal(2, cart.ItemCount);
        Assert.Equal(19.98m, cart.Subtotal);
    }

    [Fact]
    public void Adding_same_variant_merges_quantity_and_keeps_captured_price()
    {
        var cart = NewCart();
        var (p, v) = Ids();
        cart.AddItem(p, v, "SKU-1", "Widget", 10m, "USD", 1, Now);

        // Even if the "current" price were different, the captured price from the first add is kept.
        cart.AddItem(p, v, "SKU-1", "Widget", 999m, "USD", 2, Now);

        Assert.Single(cart.Items);
        Assert.Equal(3, cart.ItemCount);
        Assert.Equal(30m, cart.Subtotal); // 3 * captured 10
    }

    [Fact]
    public void Adding_a_different_currency_is_rejected()
    {
        var cart = NewCart();
        var (p1, v1) = Ids();
        var (p2, v2) = Ids();
        cart.AddItem(p1, v1, "SKU-1", "Widget", 10m, "USD", 1, Now);

        var result = cart.AddItem(p2, v2, "SKU-2", "Gadget", 5m, "EUR", 1, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("cart.currency_mismatch", result.Error.Code);
        Assert.Single(cart.Items);
    }

    [Fact]
    public void Setting_quantity_to_zero_removes_the_line()
    {
        var cart = NewCart();
        var (p, v) = Ids();
        cart.AddItem(p, v, "SKU-1", "Widget", 10m, "USD", 3, Now);
        var itemId = cart.Items.First().Id;

        var result = cart.SetItemQuantity(itemId, 0, Now);

        Assert.True(result.IsSuccess);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Updating_unknown_item_fails()
    {
        var cart = NewCart();

        var result = cart.SetItemQuantity(Guid.NewGuid(), 2, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("cart.item.not_found", result.Error.Code);
    }

    [Fact]
    public void Clear_empties_items_and_resets_currency()
    {
        var cart = NewCart();
        var (p, v) = Ids();
        cart.AddItem(p, v, "SKU-1", "Widget", 10m, "USD", 1, Now);

        cart.Clear(Now);

        Assert.Empty(cart.Items);
        Assert.Equal(string.Empty, cart.Currency);
    }

    [Fact]
    public void Adding_renews_the_sliding_expiry()
    {
        var cart = NewCart();
        var (p, v) = Ids();
        var later = Now.AddDays(3);

        cart.AddItem(p, v, "SKU-1", "Widget", 10m, "USD", 1, later);

        Assert.Equal(later.AddDays(Catrashop.Modules.Cart.Domain.Cart.ExpiryDays), cart.ExpiresAtUtc);
        Assert.False(cart.IsExpired(later));
    }

    [Fact]
    public void A_checked_out_cart_rejects_further_changes()
    {
        var cart = NewCart();
        var (p, v) = Ids();
        cart.AddItem(p, v, "SKU-1", "Widget", 10m, "USD", 1, Now);
        cart.MarkCheckedOut(Now);

        var result = cart.AddItem(p, v, "SKU-1", "Widget", 10m, "USD", 1, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("cart.not_active", result.Error.Code);
    }
}
