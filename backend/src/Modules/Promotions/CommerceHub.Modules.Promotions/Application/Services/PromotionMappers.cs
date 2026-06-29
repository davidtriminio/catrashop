using CommerceHub.Modules.Promotions.Application.Contracts;
using CommerceHub.Modules.Promotions.Domain;

namespace CommerceHub.Modules.Promotions.Application.Services;

internal static class PromotionMappers
{
    public static CouponResponse ToResponse(Coupon c) =>
        new(c.Id, c.Code, c.Name, c.Description, c.DiscountType.ToString(), c.DiscountValue, c.Currency,
            c.MinimumSubtotal, c.MaxDiscountAmount, c.StartsAtUtc, c.EndsAtUtc, c.UsageLimit, c.PerCustomerLimit,
            c.TimesRedeemed, c.IsActive, c.CreatedAtUtc);
}
