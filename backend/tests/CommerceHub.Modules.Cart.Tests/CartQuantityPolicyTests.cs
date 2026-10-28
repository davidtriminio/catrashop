using CommerceHub.Modules.Cart.Application.Services;
using Xunit;

namespace CommerceHub.Modules.Cart.Tests;

public class CartQuantityPolicyTests
{
    private const int Max = 20;

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Below_one_is_rejected(int quantity)
    {
        var result = CartQuantityPolicy.Check(quantity, Max, 100);
        Assert.True(result.IsFailure);
        Assert.Equal("cart.quantity_invalid", result.Error.Code);
    }

    [Fact]
    public void Exactly_the_line_cap_is_allowed_and_one_more_is_not()
    {
        Assert.True(CartQuantityPolicy.Check(Max, Max, 100).IsSuccess);
        var over = CartQuantityPolicy.Check(Max + 1, Max, 100);
        Assert.Equal("cart.quantity_exceeds_limit", over.Error.Code);
    }

    [Fact]
    public void Never_more_than_the_available_stock()
    {
        Assert.True(CartQuantityPolicy.Check(3, Max, 3).IsSuccess);
        var over = CartQuantityPolicy.Check(4, Max, 3);
        Assert.Equal("cart.insufficient_stock", over.Error.Code);
        Assert.Contains("3", over.Error.Message);
    }

    [Fact]
    public void Out_of_stock_has_its_own_message()
    {
        var result = CartQuantityPolicy.Check(1, Max, 0);
        Assert.Equal("cart.insufficient_stock", result.Error.Code);
        Assert.Contains("out of stock", result.Error.Message);
    }

    [Fact]
    public void Untracked_stock_only_applies_the_line_cap()
    {
        Assert.True(CartQuantityPolicy.Check(Max, Max, null).IsSuccess);
    }

    [Fact]
    public void The_line_cap_wins_over_plentiful_stock()
    {
        var result = CartQuantityPolicy.Check(Max + 5, Max, 1000);
        Assert.Equal("cart.quantity_exceeds_limit", result.Error.Code);
    }
}
