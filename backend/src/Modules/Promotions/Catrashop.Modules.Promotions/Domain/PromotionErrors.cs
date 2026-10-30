using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Promotions.Domain;

public static class PromotionErrors
{
    // --- Coupon administration ---------------------------------------------------------------
    public static Error CodeInUse(string code) =>
        Error.Conflict("promotions.coupon.code_in_use", $"A coupon with code '{code}' already exists.");

    public static readonly Error NotFoundById =
        Error.NotFound("promotions.coupon.not_found", "Coupon not found.");

    public static Error NotFound(string code) =>
        Error.NotFound("promotions.coupon.not_found", $"Coupon '{code}' was not found.");

    public static Error InvalidPercentage =
        Error.Validation("promotions.coupon.invalid_percentage", "A percentage discount must be between 0 and 100.");

    public static Error InvalidAmount =
        Error.Validation("promotions.coupon.invalid_amount", "A discount amount must be greater than zero.");

    public static Error FixedAmountNeedsCurrency =
        Error.Validation("promotions.coupon.currency_required", "A fixed-amount coupon requires a currency.");

    // --- Redemption / evaluation -------------------------------------------------------------
    public static readonly Error Inactive =
        Error.Conflict("promotions.coupon.inactive", "This coupon is not active.");

    public static readonly Error NotStarted =
        Error.Conflict("promotions.coupon.not_started", "This coupon is not valid yet.");

    public static readonly Error Expired =
        Error.Conflict("promotions.coupon.expired", "This coupon has expired.");

    public static Error MinimumSubtotal(decimal minimum, string currency) =>
        Error.Conflict("promotions.coupon.minimum_subtotal", $"This coupon requires a minimum subtotal of {minimum} {currency}.");

    public static readonly Error UsageLimitReached =
        Error.Conflict("promotions.coupon.usage_limit_reached", "This coupon has reached its usage limit.");

    public static readonly Error CustomerLimitReached =
        Error.Conflict("promotions.coupon.customer_limit_reached", "You have already used this coupon the maximum number of times.");

    public static readonly Error CurrencyMismatch =
        Error.Conflict("promotions.coupon.currency_mismatch", "This coupon cannot be applied in the order's currency.");
}
