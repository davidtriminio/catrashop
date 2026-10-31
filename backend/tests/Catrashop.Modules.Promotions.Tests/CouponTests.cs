using Catrashop.Modules.Promotions.Domain;
using Xunit;

namespace Catrashop.Modules.Promotions.Tests;

public class CouponTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    private static Coupon Percentage(decimal percent, decimal? cap = null, decimal min = 0m,
        int? usageLimit = null, int? perCustomer = null, DateTimeOffset? starts = null, DateTimeOffset? ends = null) =>
        Coupon.Create("save", "Save", null, DiscountType.Percentage, percent, null, min, cap,
            starts, ends, usageLimit, perCustomer, Now).Value;

    private static Coupon Fixed(decimal amount, string currency = "USD", decimal min = 0m) =>
        Coupon.Create("ten", "Ten off", null, DiscountType.FixedAmount, amount, currency, min, null,
            null, null, null, null, Now).Value;

    [Fact]
    public void Create_normalizes_code_to_upper_and_starts_active()
    {
        var coupon = Coupon.Create("  spring-sale ", "Spring", null, DiscountType.Percentage, 15m, null, 0m, null,
            null, null, null, null, Now);

        Assert.True(coupon.IsSuccess);
        Assert.Equal("SPRING-SALE", coupon.Value.Code);
        Assert.True(coupon.Value.IsActive);
        Assert.Equal(0, coupon.Value.TimesRedeemed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void Percentage_must_be_between_1_and_100(decimal percent)
    {
        var result = Coupon.Create("x", "x", null, DiscountType.Percentage, percent, null, 0m, null,
            null, null, null, null, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("promotions.coupon.invalid_percentage", result.Error.Code);
    }

    [Fact]
    public void Fixed_amount_requires_a_currency()
    {
        var result = Coupon.Create("x", "x", null, DiscountType.FixedAmount, 10m, null, 0m, null,
            null, null, null, null, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("promotions.coupon.currency_required", result.Error.Code);
    }

    [Fact]
    public void Percentage_discount_is_rounded_to_two_places()
    {
        var coupon = Percentage(10m);

        var discount = coupon.CalculateDiscount(99.99m, "USD", Now, 0);

        Assert.True(discount.IsSuccess);
        Assert.Equal(10.00m, discount.Value); // 9.999 -> 10.00 (away from zero)
    }

    [Fact]
    public void Percentage_discount_respects_its_cap()
    {
        var coupon = Percentage(50m, cap: 20m);

        var discount = coupon.CalculateDiscount(100m, "USD", Now, 0);

        Assert.Equal(20m, discount.Value); // 50 capped at 20
    }

    [Fact]
    public void Fixed_discount_never_exceeds_the_subtotal()
    {
        var coupon = Fixed(30m);

        var discount = coupon.CalculateDiscount(25m, "USD", Now, 0);

        Assert.Equal(25m, discount.Value);
    }

    [Fact]
    public void Fixed_discount_currency_must_match_the_order()
    {
        var coupon = Fixed(10m, "USD");

        var discount = coupon.CalculateDiscount(100m, "EUR", Now, 0);

        Assert.True(discount.IsFailure);
        Assert.Equal("promotions.coupon.currency_mismatch", discount.Error.Code);
    }

    [Fact]
    public void Minimum_subtotal_gates_the_discount()
    {
        var coupon = Percentage(10m, min: 50m);

        Assert.True(coupon.CalculateDiscount(49.99m, "USD", Now, 0).IsFailure);
        Assert.True(coupon.CalculateDiscount(50m, "USD", Now, 0).IsSuccess);
    }

    [Fact]
    public void Inactive_coupon_does_not_apply()
    {
        var coupon = Percentage(10m);
        coupon.Deactivate(Now);

        var discount = coupon.CalculateDiscount(100m, "USD", Now, 0);

        Assert.True(discount.IsFailure);
        Assert.Equal("promotions.coupon.inactive", discount.Error.Code);
    }

    [Fact]
    public void Coupon_is_not_valid_before_its_window_or_after_it()
    {
        var coupon = Percentage(10m, starts: Now.AddDays(1), ends: Now.AddDays(2));

        Assert.Equal("promotions.coupon.not_started", coupon.CalculateDiscount(100m, "USD", Now, 0).Error.Code);
        Assert.Equal("promotions.coupon.expired", coupon.CalculateDiscount(100m, "USD", Now.AddDays(3), 0).Error.Code);
        Assert.True(coupon.CalculateDiscount(100m, "USD", Now.AddDays(1).AddHours(1), 0).IsSuccess);
    }

    [Fact]
    public void End_of_window_is_exclusive()
    {
        var coupon = Percentage(10m, ends: Now);

        Assert.Equal("promotions.coupon.expired", coupon.CalculateDiscount(100m, "USD", Now, 0).Error.Code);
    }

    [Fact]
    public void Global_usage_limit_is_enforced_via_the_counter()
    {
        var coupon = Percentage(10m, usageLimit: 1);
        coupon.RecordRedemption(Now);

        var discount = coupon.CalculateDiscount(100m, "USD", Now, 0);

        Assert.True(discount.IsFailure);
        Assert.Equal("promotions.coupon.usage_limit_reached", discount.Error.Code);
    }

    [Fact]
    public void Per_customer_limit_is_enforced()
    {
        var coupon = Percentage(10m, perCustomer: 2);

        Assert.True(coupon.CalculateDiscount(100m, "USD", Now, 1).IsSuccess);   // 2nd use allowed
        Assert.True(coupon.CalculateDiscount(100m, "USD", Now, 2).IsFailure);   // 3rd use blocked
    }

    [Fact]
    public void Releasing_a_redemption_frees_a_slot_and_never_goes_negative()
    {
        var coupon = Percentage(10m, usageLimit: 1);
        coupon.RecordRedemption(Now);
        Assert.Equal(1, coupon.TimesRedeemed);

        coupon.ReleaseRedemption(Now);
        Assert.Equal(0, coupon.TimesRedeemed);

        coupon.ReleaseRedemption(Now); // idempotent floor
        Assert.Equal(0, coupon.TimesRedeemed);

        Assert.True(coupon.CalculateDiscount(100m, "USD", Now, 0).IsSuccess); // slot freed
    }

    [Fact]
    public void Redemption_release_is_idempotent()
    {
        var r = new CouponRedemption(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SAVE", 10m, "USD", Now);

        Assert.False(r.IsReleased);
        r.Release(Now);
        var firstReleaseAt = r.ReleasedAtUtc;
        r.Release(Now.AddDays(1));

        Assert.True(r.IsReleased);
        Assert.Equal(firstReleaseAt, r.ReleasedAtUtc); // second release does not move the timestamp
    }
}
