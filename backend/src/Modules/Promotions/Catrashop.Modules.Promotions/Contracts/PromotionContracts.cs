using Catrashop.BuildingBlocks.Messaging;
using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Promotions.Contracts;

/// <summary>
/// Public, in-process contract the Orders module calls at checkout. <see cref="QuoteAsync"/> evaluates a coupon
/// without writing anything (for cart preview and to compute the order's discount before it is created);
/// <see cref="RedeemAsync"/> commits a redemption for a placed order and is idempotent on
/// <c>reference</c> (the cart id), so a retried checkout never double-counts; <see cref="ReleaseAsync"/> gives
/// the redemption back when the order is cancelled or refunded.
///
/// <para>Promotions never references Orders: it only receives opaque identifiers, so the dependency stays
/// one-way (Orders → Promotions).</para>
/// </summary>
public interface IPromotionApplication
{
    /// <summary>Evaluate a coupon for a customer and subtotal. Read-only; returns the discount it would apply.</summary>
    Task<Result<DiscountQuote>> QuoteAsync(
        string couponCode, Guid customerId, decimal subtotal, string currency, CancellationToken ct = default);

    /// <summary>
    /// Commit a coupon redemption for an order identified by <paramref name="reference"/> (the cart id).
    /// Idempotent: a repeat for the same reference returns the already-recorded discount without counting again.
    /// </summary>
    Task<Result<AppliedDiscount>> RedeemAsync(
        string couponCode, Guid customerId, Guid reference, decimal subtotal, string currency, CancellationToken ct = default);

    /// <summary>Reverse the redemption for an order reference, if any (idempotent no-op when there is none).</summary>
    Task<Result> ReleaseAsync(Guid reference, CancellationToken ct = default);
}

public sealed record DiscountQuote(string CouponCode, decimal DiscountAmount, string Currency);

public sealed record AppliedDiscount(string CouponCode, decimal DiscountAmount, string Currency);

// --- Integration events (published via the outbox) ----------------------------------------------

public sealed record CouponRedeemedIntegrationEvent(
    Guid CouponId, string CouponCode, Guid CustomerId, Guid Reference, decimal DiscountAmount, string Currency) : IntegrationEvent;
