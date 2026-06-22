using CommerceHub.BuildingBlocks.Domain;

namespace CommerceHub.Modules.Promotions.Domain;

/// <summary>
/// An immutable record that a coupon was redeemed against one order. <see cref="Reference"/> is the caller's
/// stable identifier for that order (the checkout uses the cart id, which is also the order's idempotency key);
/// a unique (CouponId, Reference) index makes redemption idempotent, so a retried checkout can never count the
/// same order twice. Rows are also counted per customer to enforce the coupon's per-customer limit.
///
/// <para><see cref="ReleasedAtUtc"/> is set when the redemption is reversed (order cancelled/refunded); a
/// released row is kept for audit but no longer counts toward limits.</para>
/// </summary>
public sealed class CouponRedemption : Entity<Guid>
{
    private CouponRedemption() { } // EF

    public CouponRedemption(Guid id, Guid couponId, Guid customerId, Guid reference, string couponCode,
        decimal discountAmount, string currency, DateTimeOffset redeemedAtUtc) : base(id)
    {
        CouponId = couponId;
        CustomerId = customerId;
        Reference = reference;
        CouponCode = couponCode;
        DiscountAmount = discountAmount;
        Currency = currency;
        RedeemedAtUtc = redeemedAtUtc;
    }

    public Guid CouponId { get; private set; }
    public Guid CustomerId { get; private set; }

    /// <summary>The order reference this redemption belongs to (the originating cart id at checkout).</summary>
    public Guid Reference { get; private set; }

    public string CouponCode { get; private set; } = default!;
    public decimal DiscountAmount { get; private set; }
    public string Currency { get; private set; } = default!;
    public DateTimeOffset RedeemedAtUtc { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }

    public bool IsReleased => ReleasedAtUtc is not null;

    public void Release(DateTimeOffset nowUtc) => ReleasedAtUtc ??= nowUtc;
}
