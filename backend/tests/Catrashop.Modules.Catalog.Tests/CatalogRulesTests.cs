using Catrashop.BuildingBlocks.Money;
using Catrashop.BuildingBlocks.Pagination;
using Catrashop.Modules.Catalog.Application.Contracts;
using Catrashop.Modules.Catalog.Application.Services;
using Catrashop.Modules.Catalog.Application.Validation;
using Microsoft.Extensions.Options;
using Xunit;

namespace Catrashop.Modules.Catalog.Tests;

public class PriceFilterTests
{
    [Fact]
    public void Negative_bounds_are_dropped()
    {
        Assert.Equal((null, 50m), PriceFilter.Normalize(-5m, 50m));
        Assert.Equal((10m, null), PriceFilter.Normalize(10m, -1m));
    }

    [Fact]
    public void Inverted_bounds_are_swapped()
    {
        Assert.Equal((10m, 50m), PriceFilter.Normalize(50m, 10m));
    }

    [Fact]
    public void Valid_and_missing_bounds_pass_through()
    {
        Assert.Equal((10m, 50m), PriceFilter.Normalize(10m, 50m));
        Assert.Equal((null, null), PriceFilter.Normalize(null, null));
        Assert.Equal((0m, null), PriceFilter.Normalize(0m, null));
    }
}

public class PageRequestTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(-3, 20)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(100000, 100)]
    public void Page_size_is_clamped_by_the_server(int requested, int expected)
    {
        Assert.Equal(expected, new PageRequest(1, requested).PageSize);
    }

    [Fact]
    public void Page_below_one_becomes_the_first_page()
    {
        var request = new PageRequest(0, 10);
        Assert.Equal(1, request.Page);
        Assert.Equal(0, request.Skip);
    }
}

public class ProductVariantValidationTests
{
    private static readonly AddVariantRequestValidator Validator = new(Options.Create(new StoreOptions()));

    private static AddVariantRequest Variant(decimal price, string sku = "SKU-1", string name = "Default") =>
        new(sku, name, price, "HNL");

    [Theory]
    [InlineData(0.01)]
    [InlineData(19.99)]
    [InlineData(1000000)]
    public void Valid_prices_are_accepted(double price) =>
        Assert.True(Validator.Validate(Variant((decimal)price)).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000000.01)]
    public void Zero_negative_and_absurd_prices_are_rejected(double price) =>
        Assert.False(Validator.Validate(Variant((decimal)price)).IsValid);

    [Theory]
    [InlineData("HNL", true)]
    [InlineData("hnl", true)]
    [InlineData("USD", false)]
    [InlineData("EUR", false)]
    public void Only_the_store_currency_is_accepted(string currency, bool valid) =>
        Assert.Equal(valid, Validator.Validate(new AddVariantRequest("SKU-1", "Default", 5m, currency)).IsValid);

    [Fact]
    public void The_store_currency_is_configurable()
    {
        var usd = new AddVariantRequestValidator(Options.Create(new StoreOptions { Currency = "USD" }));
        Assert.True(usd.Validate(new AddVariantRequest("SKU-1", "Default", 5m, "USD")).IsValid);
        Assert.False(usd.Validate(new AddVariantRequest("SKU-1", "Default", 5m, "HNL")).IsValid);
    }

    [Fact]
    public void More_than_two_decimals_are_rejected() =>
        Assert.False(Validator.Validate(Variant(9.999m)).IsValid);

    [Fact]
    public void Sku_and_name_lengths_are_capped()
    {
        Assert.False(Validator.Validate(Variant(5m, sku: new string('A', 65))).IsValid);
        Assert.True(Validator.Validate(Variant(5m, sku: new string('A', 64))).IsValid);
        Assert.False(Validator.Validate(Variant(5m, name: new string('n', 151))).IsValid);
    }

    [Fact]
    public void Product_name_and_description_lengths_are_capped()
    {
        var validator = new CreateProductRequestValidator(Options.Create(new StoreOptions()));
        var variants = new[] { new ProductVariantInput("SKU-1", "Default", 5m, "HNL") };

        CreateProductRequest Product(string name, string description) =>
            new(name, description, Guid.NewGuid(), null, variants, null, null);

        Assert.True(validator.Validate(Product(new string('a', 200), new string('d', 8000))).IsValid);
        Assert.False(validator.Validate(Product(new string('a', 201), "d")).IsValid);
        Assert.False(validator.Validate(Product("a", new string('d', 8001))).IsValid);
    }
}
