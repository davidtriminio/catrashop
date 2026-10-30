using Catrashop.BuildingBlocks.Messaging;
using Catrashop.Infrastructure.Messaging;
using Catrashop.Modules.Identity.Contracts;
using Catrashop.Modules.Identity.Infrastructure.Persistence;
using Catrashop.Modules.Inventory.Contracts;
using Catrashop.Modules.Inventory.Infrastructure.Persistence;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Orders.Infrastructure.Persistence;
using Catrashop.Modules.Payments.Contracts;
using Catrashop.Modules.Payments.Infrastructure.Persistence;
using Catrashop.Modules.Promotions.Contracts;
using Catrashop.Modules.Promotions.Infrastructure.Persistence;
using Catrashop.Modules.Shipping.Contracts;
using Catrashop.Modules.Shipping.Infrastructure.Persistence;

namespace Catrashop.Api.Integration;

/// <summary>
/// Wires the messaging pipeline at the composition root: the bus + background services, one outbox drain per
/// publishing module, the integration-event handlers (cross-module reactions live here so no module depends on
/// another's events), and the event-type registry the consumer binds and deserializes against.
/// </summary>
public static class IntegrationSetup
{
    public static IServiceCollection AddCatrashopMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMessaging(configuration);

        // One outbox drain per module that publishes events.
        services.AddOutboxProcessor<IdentityDbContext>();
        services.AddOutboxProcessor<InventoryDbContext>();
        services.AddOutboxProcessor<OrdersDbContext>();
        services.AddOutboxProcessor<PaymentsDbContext>();
        services.AddOutboxProcessor<PromotionsDbContext>();
        services.AddOutboxProcessor<ShippingDbContext>();

        // Cross-module handlers (consumer side).
        services.AddScoped<IIntegrationEventHandler<CustomerRegisteredIntegrationEvent>, CustomerRegisteredHandler>();
        services.AddScoped<IIntegrationEventHandler<OrderCreatedIntegrationEvent>, OrderCreatedNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<OrderPaidIntegrationEvent>, OrderPaidNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<OrderCancelledIntegrationEvent>, OrderCancelledNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedIntegrationEvent>, PaymentFailedNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<ShipmentCreatedIntegrationEvent>, ShipmentCreatedNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<ShipmentDeliveredIntegrationEvent>, ShipmentDeliveredNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<LowInventoryDetectedIntegrationEvent>, LowInventoryLoggingHandler>();
        services.AddScoped<IIntegrationEventHandler<CouponRedeemedIntegrationEvent>, CouponRedeemedAnalyticsHandler>();

        return services;
    }

    /// <summary>
    /// Registers every integration-event type by name so the consumer can bind its queue and deserialize.
    /// Call after the host is built (the registry is a singleton). Types without a handler are still registered
    /// so publishing works and consumers are future-proof; the dispatcher ignores those on receipt.
    /// </summary>
    public static void RegisterIntegrationEventTypes(IntegrationEventTypeRegistry registry)
    {
        registry.Register<CustomerRegisteredIntegrationEvent>();
        registry.Register<PasswordResetRequestedIntegrationEvent>();
        registry.Register<InventoryReservedIntegrationEvent>();
        registry.Register<InventoryReleasedIntegrationEvent>();
        registry.Register<LowInventoryDetectedIntegrationEvent>();
        registry.Register<OrderCreatedIntegrationEvent>();
        registry.Register<OrderPaidIntegrationEvent>();
        registry.Register<OrderCancelledIntegrationEvent>();
        registry.Register<PaymentCapturedIntegrationEvent>();
        registry.Register<PaymentFailedIntegrationEvent>();
        registry.Register<RefundCompletedIntegrationEvent>();
        registry.Register<CouponRedeemedIntegrationEvent>();
        registry.Register<ShipmentCreatedIntegrationEvent>();
        registry.Register<ShipmentDeliveredIntegrationEvent>();
    }
}
