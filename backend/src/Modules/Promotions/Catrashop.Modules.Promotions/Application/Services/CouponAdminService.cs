using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Promotions.Application.Abstractions;
using Catrashop.Modules.Promotions.Application.Contracts;
using Catrashop.Modules.Promotions.Domain;

namespace Catrashop.Modules.Promotions.Application.Services;

/// <summary>Administration of coupons (create/update/activate/list). Guarded by <c>promotions.manage</c>.</summary>
public sealed class CouponAdminService
{
    private readonly ICouponRepository _coupons;
    private readonly IPromotionsUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public CouponAdminService(ICouponRepository coupons, IPromotionsUnitOfWork unitOfWork, TimeProvider clock)
    {
        _coupons = coupons;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CouponResponse>> CreateAsync(CreateCouponRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _coupons.CodeExistsAsync(code, ct))
            return PromotionErrors.CodeInUse(code);

        var result = Coupon.Create(code, request.Name, request.Description, request.DiscountType,
            request.DiscountValue, request.Currency, request.MinimumSubtotal, request.MaxDiscountAmount,
            request.StartsAtUtc, request.EndsAtUtc, request.UsageLimit, request.PerCustomerLimit, _clock.GetUtcNow());
        if (result.IsFailure)
            return result.Error;

        _coupons.Add(result.Value);
        await _unitOfWork.SaveChangesAsync(ct);
        return PromotionMappers.ToResponse(result.Value);
    }

    public async Task<Result<CouponResponse>> UpdateAsync(Guid id, UpdateCouponRequest request, CancellationToken ct = default)
    {
        var coupon = await _coupons.GetByIdAsync(id, ct);
        if (coupon is null)
            return PromotionErrors.NotFoundById;

        var result = coupon.Update(request.Name, request.Description, request.DiscountType, request.DiscountValue,
            request.Currency, request.MinimumSubtotal, request.MaxDiscountAmount, request.StartsAtUtc,
            request.EndsAtUtc, request.UsageLimit, request.PerCustomerLimit, _clock.GetUtcNow());
        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(ct);
        return PromotionMappers.ToResponse(coupon);
    }

    public async Task<Result> SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        var coupon = await _coupons.GetByIdAsync(id, ct);
        if (coupon is null)
            return PromotionErrors.NotFoundById;

        if (active) coupon.Activate(_clock.GetUtcNow());
        else coupon.Deactivate(_clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<CouponResponse>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var coupon = await _coupons.GetByIdAsync(id, ct);
        return coupon is null ? PromotionErrors.NotFoundById : PromotionMappers.ToResponse(coupon);
    }

    public async Task<IReadOnlyList<CouponResponse>> ListAsync(CancellationToken ct = default) =>
        (await _coupons.ListAsync(ct)).Select(PromotionMappers.ToResponse).ToArray();
}
