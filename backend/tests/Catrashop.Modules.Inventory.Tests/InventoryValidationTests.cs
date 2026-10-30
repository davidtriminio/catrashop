using Catrashop.Modules.Inventory.Application.Contracts;
using Catrashop.Modules.Inventory.Application.Validation;
using Xunit;

namespace Catrashop.Modules.Inventory.Tests;

public class InventoryValidationTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(10, 5, true)]
    [InlineData(StockLimits.MaxUnits, StockLimits.MaxUnits, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(StockLimits.MaxUnits + 1, 0, false)]
    [InlineData(0, StockLimits.MaxUnits + 1, false)]
    public void Initial_stock_and_threshold_must_be_within_range(int onHand, int threshold, bool valid)
    {
        var result = new CreateInventoryItemRequestValidator().Validate(new CreateInventoryItemRequest("SKU-1", onHand, threshold));
        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void Sku_length_is_capped()
    {
        var validator = new CreateInventoryItemRequestValidator();
        Assert.True(validator.Validate(new CreateInventoryItemRequest(new string('S', 64), 1, 1)).IsValid);
        Assert.False(validator.Validate(new CreateInventoryItemRequest(new string('S', 65), 1, 1)).IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    [InlineData(StockLimits.MaxUnits + 1, false)]
    public void Received_quantity_must_be_positive_and_bounded(int quantity, bool valid) =>
        Assert.Equal(valid, new ReceiveStockRequestValidator().Validate(new ReceiveStockRequest(quantity, null)).IsValid);

    [Theory]
    [InlineData(5, true)]
    [InlineData(-5, true)]
    [InlineData(0, false)]
    [InlineData(StockLimits.MaxUnits + 1, false)]
    [InlineData(-StockLimits.MaxUnits - 1, false)]
    public void Adjustment_delta_must_be_non_zero_and_bounded(int delta, bool valid) =>
        Assert.Equal(valid, new AdjustStockRequestValidator().Validate(new AdjustStockRequest(delta, null)).IsValid);

    [Theory]
    [InlineData(0, true)]
    [InlineData(25, true)]
    [InlineData(-1, false)]
    [InlineData(StockLimits.MaxUnits + 1, false)]
    public void Low_stock_threshold_must_be_non_negative_and_bounded(int threshold, bool valid) =>
        Assert.Equal(valid, new SetThresholdRequestValidator().Validate(new SetThresholdRequest(threshold)).IsValid);

    [Fact]
    public void Stock_never_goes_negative()
    {
        var item = Domain.InventoryItem.Create("SKU-1", 3, 1, DateTimeOffset.UtcNow);
        var result = item.Adjust(-10, "shrinkage", DateTimeOffset.UtcNow);
        Assert.True(result.IsFailure);
        Assert.Equal(3, item.QuantityOnHand);
    }
}
