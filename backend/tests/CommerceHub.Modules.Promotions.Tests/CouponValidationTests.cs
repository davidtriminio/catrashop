using CommerceHub.Modules.Promotions.Application.Contracts;
using CommerceHub.Modules.Promotions.Application.Validation;
using CommerceHub.Modules.Promotions.Domain;
using Xunit;

namespace CommerceHub.Modules.Promotions.Tests;

public class CouponValidationTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly CreateCouponRequestValidator Validator = new();

    private static CreateCouponRequest Percent(decimal value, DateTimeOffset? starts = null, DateTimeOffset? ends = null,
        int? usage = null, int? perCustomer = null, decimal minimum = 0m, decimal? cap = null) =>
        new("SAVE", "Save", null, DiscountType.Percentage, value, null, minimum, cap, starts, ends, usage, perCustomer);

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    [InlineData(100.01, false)]
    public void Percentage_must_be_between_0_exclusive_and_100(double value, bool valid) =>
        Assert.Equal(valid, Validator.Validate(Percent((decimal)value)).IsValid);

    [Fact]
    public void Fixed_amount_needs_a_positive_value_and_a_currency()
    {
        var ok = new CreateCouponRequest("TEN", "Ten", null, DiscountType.FixedAmount, 10m, "USD", 0m, null, null, null, null, null);
        Assert.True(Validator.Validate(ok).IsValid);
        Assert.False(Validator.Validate(ok with { Currency = null }).IsValid);
        Assert.False(Validator.Validate(ok with { DiscountValue = 0m }).IsValid);
    }

    [Fact]
    public void End_date_must_be_after_start_date()
    {
        Assert.True(Validator.Validate(Percent(10, Start, Start.AddDays(1))).IsValid);
        Assert.False(Validator.Validate(Percent(10, Start, Start)).IsValid);
        Assert.False(Validator.Validate(Percent(10, Start, Start.AddDays(-1))).IsValid);
        Assert.True(Validator.Validate(Percent(10, null, Start)).IsValid);
    }

    [Fact]
    public void Limits_and_minimums_must_be_sensible()
    {
        Assert.False(Validator.Validate(Percent(10, usage: 0)).IsValid);
        Assert.False(Validator.Validate(Percent(10, perCustomer: -1)).IsValid);
        Assert.False(Validator.Validate(Percent(10, minimum: -1m)).IsValid);
        Assert.False(Validator.Validate(Percent(10, cap: 0m)).IsValid);
        Assert.True(Validator.Validate(Percent(10, usage: 100, perCustomer: 1, minimum: 25m, cap: 5m)).IsValid);
    }

    [Fact]
    public void Code_and_name_lengths_are_capped()
    {
        Assert.False(Validator.Validate(Percent(10) with { Code = new string('C', 41) }).IsValid);
        Assert.False(Validator.Validate(Percent(10) with { Name = new string('N', 121) }).IsValid);
        Assert.False(Validator.Validate(Percent(10) with { Description = new string('D', 501) }).IsValid);
    }
}
