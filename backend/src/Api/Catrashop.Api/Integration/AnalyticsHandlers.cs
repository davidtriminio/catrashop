using Catrashop.BuildingBlocks.Messaging;
using Catrashop.Modules.Inventory.Contracts;
using Catrashop.Modules.Promotions.Contracts;

namespace Catrashop.Api.Integration;

/// <summary>
/// Lightweight observers that demonstrate fan-out to more than one consumer: operational logging for low stock
/// and coupon-redemption analytics. Real implementations would forward to an ops channel / analytics sink;
/// here they log, and are trivially idempotent.
/// </summary>
public sealed class LowInventoryLoggingHandler : IIntegrationEventHandler<LowInventoryDetectedIntegrationEvent>
{
    private readonly ILogger<LowInventoryLoggingHandler> _logger;
    public LowInventoryLoggingHandler(ILogger<LowInventoryLoggingHandler> logger) => _logger = logger;

    public Task HandleAsync(LowInventoryDetectedIntegrationEvent e, CancellationToken ct = default)
    {
        _logger.LogWarning("Low stock: SKU {Sku} available {Available} at/below threshold {Threshold}.", e.Sku, e.Available, e.Threshold);
        return Task.CompletedTask;
    }
}

public sealed class CouponRedeemedAnalyticsHandler : IIntegrationEventHandler<CouponRedeemedIntegrationEvent>
{
    private readonly ILogger<CouponRedeemedAnalyticsHandler> _logger;
    public CouponRedeemedAnalyticsHandler(ILogger<CouponRedeemedAnalyticsHandler> logger) => _logger = logger;

    public Task HandleAsync(CouponRedeemedIntegrationEvent e, CancellationToken ct = default)
    {
        _logger.LogInformation("Coupon redeemed: {Code} by {CustomerId} for {Amount} {Currency}.", e.CouponCode, e.CustomerId, e.DiscountAmount, e.Currency);
        return Task.CompletedTask;
    }
}
