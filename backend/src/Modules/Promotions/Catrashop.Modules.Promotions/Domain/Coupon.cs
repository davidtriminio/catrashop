using Catrashop.BuildingBlocks.Domain;
using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Promotions.Domain;

/// <summary>
/// A discount coupon. It owns the rules that decide whether it applies to a given order (active flag, validity
/// window, minimum subtotal, currency, global and per-customer usage limits) and how much it takes off
/// (<see cref="DiscountType"/> + value, with an optional cap for percentages). All of that logic lives in
/// <see cref="CalculateDiscount"/> so it is decided in one place and unit-testable without a database.
///
/// <para><see cref="TimesRedeemed"/> is a denormalized counter used to enforce the global usage limit cheaply;
/// individual redemptions are recorded as separate <see cref="CouponRedemption"/> rows (one per order), whose
/// unique (coupon, reference) index is the real guard against counting the same order twice.</para>
/// </summary>
public sealed class Coupon : AggregateRoot<Guid>
{
    private Coupon() { } // EF

    private Coupon(Guid id, string code, string name, string? description, DiscountType discountType,
        decimal discountValue, string? currency, decimal minimumSubtotal, decimal? maxDiscountAmount,
        DateTimeOffset? startsAtUtc, DateTimeOffset? endsAtUtc, int? usageLimit, int? perCustomerLimit,
        DateTimeOffset nowUtc) : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        DiscountType = discountType;
        DiscountValue = discountValue;
        Currency = currency;
        MinimumSubtotal = minimumSubtotal;
        MaxDiscountAmount = maxDiscountAmount;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        UsageLimit = usageLimit;
        PerCustomerLimit = perCustomerLimit;
        TimesRedeemed = 0;
        IsActive = true;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Case-insensitive coupon code, stored normalized to upper-case.</summary>
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public DiscountType DiscountType { get; private set; }

    /// <summary>Percentage (0–100) for <see cref="DiscountType.Percentage"/>, or a money amount for fixed.</summary>
    public decimal DiscountValue { get; private set; }

    /// <summary>Required for fixed-amount coupons; the order currency must match. Null for percentage coupons.</summary>
    public string? Currency { get; private set; }

    /// <summary>Minimum order subtotal before the coupon applies (0 = no minimum).</summary>
    public decimal MinimumSubtotal { get; private set; }

    /// <summary>Optional cap on the discount a percentage coupon can produce.</summary>
    public decimal? MaxDiscountAmount { get; private set; }

    public DateTimeOffset? StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }

    /// <summary>Total redemptions allowed across all customers (null = unlimited).</summary>
    public int? UsageLimit { get; private set; }

    /// <summary>Redemptions allowed per customer (null = unlimited).</summary>
    public int? PerCustomerLimit { get; private set; }

    public int TimesRedeemed { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Result<Coupon> Create(
        string code, string name, string? description, DiscountType discountType, decimal discountValue,
        string? currency, decimal minimumSubtotal, decimal? maxDiscountAmount, DateTimeOffset? startsAtUtc,
        DateTimeOffset? endsAtUtc, int? usageLimit, int? perCustomerLimit, DateTimeOffset nowUtc)
    {
        var validation = Validate(discountType, discountValue, currency);
        if (validation.IsFailure)
            return validation.Error;

        return new Coupon(Guid.NewGuid(), Normalize(code), name.Trim(), Trim(description), discountType,
            discountValue, NormalizeCurrency(discountType, currency), Math.Max(0m, minimumSubtotal),
            maxDiscountAmount, startsAtUtc, endsAtUtc, usageLimit, perCustomerLimit, nowUtc);
    }

    public Result Update(
        string name, string? description, DiscountType discountType, decimal discountValue, string? currency,
        decimal minimumSubtotal, decimal? maxDiscountAmount, DateTimeOffset? startsAtUtc, DateTimeOffset? endsAtUtc,
        int? usageLimit, int? perCustomerLimit, DateTimeOffset nowUtc)
    {
        var validation = Validate(discountType, discountValue, currency);
        if (validation.IsFailure)
            return validation;

        Name = name.Trim();
        Description = Trim(description);
        DiscountType = discountType;
        DiscountValue = discountValue;
        Currency = NormalizeCurrency(discountType, currency);
        MinimumSubtotal = Math.Max(0m, minimumSubtotal);
        MaxDiscountAmount = maxDiscountAmount;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        UsageLimit = usageLimit;
        PerCustomerLimit = perCustomerLimit;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    public void Activate(DateTimeOffset nowUtc) { IsActive = true; UpdatedAtUtc = nowUtc; }
    public void Deactivate(DateTimeOffset nowUtc) { IsActive = false; UpdatedAtUtc = nowUtc; }

    /// <summary>
    /// Decide whether this coupon applies to an order of <paramref name="subtotal"/> in
    /// <paramref name="currency"/> for a customer who has already redeemed it
    /// <paramref name="customerRedemptionCount"/> times, and if so how much it takes off. The returned amount is
    /// rounded to 2 places and never exceeds the subtotal. All rules fail closed with a specific error.
    /// </summary>
    public Result<decimal> CalculateDiscount(decimal subtotal, string currency, DateTimeOffset nowUtc, int customerRedemptionCount)
    {
        if (!IsActive)
            return PromotionErrors.Inactive;
        if (StartsAtUtc is { } starts && nowUtc < starts)
            return PromotionErrors.NotStarted;
        if (EndsAtUtc is { } ends && nowUtc >= ends)
            return PromotionErrors.Expired;
        if (subtotal < MinimumSubtotal)
            return PromotionErrors.MinimumSubtotal(MinimumSubtotal, currency);
        if (UsageLimit is { } limit && TimesRedeemed >= limit)
            return PromotionErrors.UsageLimitReached;
        if (PerCustomerLimit is { } perCustomer && customerRedemptionCount >= perCustomer)
            return PromotionErrors.CustomerLimitReached;
        if (DiscountType == DiscountType.FixedAmount &&
            !string.Equals(Currency, currency, StringComparison.OrdinalIgnoreCase))
            return PromotionErrors.CurrencyMismatch;

        var raw = DiscountType switch
        {
            DiscountType.Percentage => ApplyCap(Math.Round(subtotal * (DiscountValue / 100m), 2, MidpointRounding.AwayFromZero)),
            DiscountType.FixedAmount => DiscountValue,
            _ => 0m
        };

        // A discount can never make an order negative.
        return Math.Min(Math.Round(raw, 2, MidpointRounding.AwayFromZero), subtotal);
    }

    /// <summary>Record one redemption against the global counter (after <see cref="CalculateDiscount"/> succeeds).</summary>
    public void RecordRedemption(DateTimeOffset nowUtc)
    {
        TimesRedeemed++;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Give a redemption slot back (e.g. the order was cancelled or refunded). Never goes below zero.</summary>
    public void ReleaseRedemption(DateTimeOffset nowUtc)
    {
        if (TimesRedeemed > 0)
            TimesRedeemed--;
        UpdatedAtUtc = nowUtc;
    }

    private decimal ApplyCap(decimal amount) =>
        MaxDiscountAmount is { } cap ? Math.Min(amount, cap) : amount;

    private static Result Validate(DiscountType discountType, decimal discountValue, string? currency)
    {
        switch (discountType)
        {
            case DiscountType.Percentage when discountValue is <= 0m or > 100m:
                return PromotionErrors.InvalidPercentage;
            case DiscountType.FixedAmount when discountValue <= 0m:
                return PromotionErrors.InvalidAmount;
            case DiscountType.FixedAmount when string.IsNullOrWhiteSpace(currency):
                return PromotionErrors.FixedAmountNeedsCurrency;
            default:
                return Result.Success();
        }
    }

    private static string Normalize(string code) => code.Trim().ToUpperInvariant();
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeCurrency(DiscountType type, string? currency) =>
        type == DiscountType.FixedAmount ? currency!.Trim().ToUpperInvariant() : null;
}
