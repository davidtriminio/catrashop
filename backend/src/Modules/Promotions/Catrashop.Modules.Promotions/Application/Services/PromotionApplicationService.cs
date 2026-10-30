using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Promotions.Application.Abstractions;
using Catrashop.Modules.Promotions.Contracts;
using Catrashop.Modules.Promotions.Domain;

namespace Catrashop.Modules.Promotions.Application.Services;

/// <summary>
/// Implements the public <see cref="IPromotionApplication"/> contract the Orders module calls at checkout.
/// The discount rules themselves live in <see cref="Coupon.CalculateDiscount"/>; this service supplies the
/// per-customer redemption count, records/releases redemptions, and keeps redemption idempotent by the order
/// reference (the cart id).
/// </summary>
public sealed class PromotionApplicationService : IPromotionApplication
{
    private readonly ICouponRepository _coupons;
    private readonly ICouponRedemptionRepository _redemptions;
    private readonly IIntegrationEventPublisher _events;
    private readonly IPromotionsUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public PromotionApplicationService(
        ICouponRepository coupons,
        ICouponRedemptionRepository redemptions,
        IIntegrationEventPublisher events,
        IPromotionsUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _coupons = coupons;
        _redemptions = redemptions;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<DiscountQuote>> QuoteAsync(
        string couponCode, Guid customerId, decimal subtotal, string currency, CancellationToken ct = default)
    {
        var evaluation = await EvaluateAsync(couponCode, customerId, subtotal, currency, ct);
        if (evaluation.IsFailure)
            return evaluation.Error;

        var (coupon, amount) = evaluation.Value;
        return new DiscountQuote(coupon.Code, amount, currency.ToUpperInvariant());
    }

    public async Task<Result<AppliedDiscount>> RedeemAsync(
        string couponCode, Guid customerId, Guid reference, decimal subtotal, string currency, CancellationToken ct = default)
    {
        // Idempotency: a redemption already recorded for this order reference wins, unchanged.
        var existing = await _redemptions.GetByReferenceAsync(reference, ct);
        if (existing is not null && !existing.IsReleased)
            return new AppliedDiscount(existing.CouponCode, existing.DiscountAmount, existing.Currency);

        var evaluation = await EvaluateAsync(couponCode, customerId, subtotal, currency, ct);
        if (evaluation.IsFailure)
            return evaluation.Error;

        var (coupon, amount) = evaluation.Value;
        var normalizedCurrency = currency.ToUpperInvariant();
        var now = _clock.GetUtcNow();

        coupon.RecordRedemption(now);
        _redemptions.Add(new CouponRedemption(
            Guid.NewGuid(), coupon.Id, customerId, reference, coupon.Code, amount, normalizedCurrency, now));
        _events.Enqueue(
            new CouponRedeemedIntegrationEvent(coupon.Id, coupon.Code, customerId, reference, amount, normalizedCurrency),
            reference);

        await _unitOfWork.SaveChangesAsync(ct);
        return new AppliedDiscount(coupon.Code, amount, normalizedCurrency);
    }

    public async Task<Result> ReleaseAsync(Guid reference, CancellationToken ct = default)
    {
        var redemption = await _redemptions.GetByReferenceAsync(reference, ct);
        if (redemption is null || redemption.IsReleased)
            return Result.Success(); // nothing to release — idempotent

        var now = _clock.GetUtcNow();
        redemption.Release(now);

        var coupon = await _coupons.GetByIdAsync(redemption.CouponId, ct);
        coupon?.ReleaseRedemption(now);

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Load the coupon by code and run its rules for this customer/subtotal; returns coupon + amount.</summary>
    private async Task<Result<(Coupon Coupon, decimal Amount)>> EvaluateAsync(
        string couponCode, Guid customerId, decimal subtotal, string currency, CancellationToken ct)
    {
        var code = couponCode.Trim().ToUpperInvariant();
        var coupon = await _coupons.GetByCodeAsync(code, ct);
        if (coupon is null)
            return PromotionErrors.NotFound(code);

        var customerCount = await _redemptions.CountForCustomerAsync(coupon.Id, customerId, ct);
        var discount = coupon.CalculateDiscount(subtotal, currency, _clock.GetUtcNow(), customerCount);
        if (discount.IsFailure)
            return discount.Error;

        return (coupon, discount.Value);
    }
}
