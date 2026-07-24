using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.Infrastructure.Messaging;
using CommerceHub.Modules.Identity.Contracts;
using CommerceHub.Modules.Identity.Infrastructure.Persistence;
using CommerceHub.Modules.Inventory.Contracts;
using CommerceHub.Modules.Inventory.Infrastructure.Persistence;
using CommerceHub.Modules.Orders.Contracts;
using CommerceHub.Modules.Orders.Infrastructure.Persistence;
using CommerceHub.Modules.Payments.Contracts;
using CommerceHub.Modules.Payments.Infrastructure.Persistence;
using CommerceHub.Modules.Promotions.Contracts;
using CommerceHub.Modules.Promotions.Infrastructure.Persistence;
using CommerceHub.Modules.Shipping.Contracts;
using CommerceHub.Modules.Shipping.Infrastructure.Persistence;

namespace CommerceHub.Api.Integration;

/// <summary>
/// Wires the messaging pipeline at the composition root: the bus + background services, one outbox drain per
/// publishing module, the integration-event handlers (cross-module reactions live here so no module depends on
/// another's events), and the event-type registry the consumer binds and deserializes against.
/// </summary>
public static class IntegrationSetup
{
    public static IServiceCollection AddCommerceHubMessaging(this IServiceCollection services, IConfiguration configuration)
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
