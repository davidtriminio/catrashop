using CommerceHub.Modules.Promotions.Application.Validation;
using CommerceHub.Modules.Promotions.Domain;

namespace CommerceHub.Modules.Promotions.Application.Contracts;

// --- Requests -----------------------------------------------------------------------------------

public sealed record CreateCouponRequest(
    string Code,
    string Name,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    string? Currency,
    decimal MinimumSubtotal,
    decimal? MaxDiscountAmount,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    int? UsageLimit,
    int? PerCustomerLimit) : ICouponWriteRequest;

public sealed record UpdateCouponRequest(
    string Name,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    string? Currency,
    decimal MinimumSubtotal,
    decimal? MaxDiscountAmount,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    int? UsageLimit,
    int? PerCustomerLimit) : ICouponWriteRequest;

/// <summary>Customer-facing coupon preview against a cart subtotal.</summary>
public sealed record QuoteCouponRequest(string CouponCode, decimal Subtotal, string Currency);

// --- Responses ----------------------------------------------------------------------------------

public sealed record CouponResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    string? Currency,
    decimal MinimumSubtotal,
    decimal? MaxDiscountAmount,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    int? UsageLimit,
    int? PerCustomerLimit,
    int TimesRedeemed,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record CouponQuoteResponse(string CouponCode, decimal DiscountAmount, string Currency);
