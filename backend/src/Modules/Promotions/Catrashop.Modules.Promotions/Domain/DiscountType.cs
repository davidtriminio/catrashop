namespace Catrashop.Modules.Promotions.Domain;

/// <summary>How a coupon reduces the order subtotal.</summary>
public enum DiscountType
{
    /// <summary>A percentage of the subtotal (0–100), optionally capped by <c>MaxDiscountAmount</c>.</summary>
    Percentage = 0,

    /// <summary>A fixed money amount in the coupon's currency, never exceeding the subtotal.</summary>
    FixedAmount = 1
}
