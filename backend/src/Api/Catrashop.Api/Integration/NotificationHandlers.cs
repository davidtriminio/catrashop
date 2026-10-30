using Catrashop.BuildingBlocks.Messaging;
using Catrashop.Modules.Notifications.Contracts;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Payments.Contracts;
using Catrashop.Modules.Shipping.Contracts;

namespace Catrashop.Api.Integration;

/// <summary>
/// Cross-module reactions that turn order/payment/shipment events into customer notifications. They live in the
/// composition root so no module depends on another's events; each keys the dispatch on the event id, so the
/// at-least-once broker never notifies twice (the Notifications module dedupes on that key).
/// </summary>
public sealed class OrderCreatedNotificationHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly INotificationDispatcher _notifications;
    public OrderCreatedNotificationHandler(INotificationDispatcher notifications) => _notifications = notifications;

    public Task HandleAsync(OrderCreatedIntegrationEvent e, CancellationToken ct = default) =>
        _notifications.DispatchAsync(new NotificationRequest(
            e.CustomerId, NotificationType.OrderPlaced, e.EventId.ToString(),
            new Dictionary<string, string> { ["orderNumber"] = e.OrderNumber }), ct);
}

public sealed class OrderPaidNotificationHandler : IIntegrationEventHandler<OrderPaidIntegrationEvent>
{
    private readonly INotificationDispatcher _notifications;
    public OrderPaidNotificationHandler(INotificationDispatcher notifications) => _notifications = notifications;

    public Task HandleAsync(OrderPaidIntegrationEvent e, CancellationToken ct = default) =>
        _notifications.DispatchAsync(new NotificationRequest(
            e.CustomerId, NotificationType.OrderPaid, e.EventId.ToString(),
            new Dictionary<string, string> { ["orderNumber"] = e.OrderNumber, ["total"] = e.GrandTotal.ToString("0.00"), ["currency"] = e.Currency }), ct);
}

public sealed class OrderCancelledNotificationHandler : IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    private readonly INotificationDispatcher _notifications;
    public OrderCancelledNotificationHandler(INotificationDispatcher notifications) => _notifications = notifications;

    public Task HandleAsync(OrderCancelledIntegrationEvent e, CancellationToken ct = default) =>
        _notifications.DispatchAsync(new NotificationRequest(
            e.CustomerId, NotificationType.OrderCancelled, e.EventId.ToString(),
            new Dictionary<string, string> { ["orderNumber"] = e.OrderNumber, ["reason"] = e.Reason ?? string.Empty }), ct);
}

public sealed class PaymentFailedNotificationHandler : IIntegrationEventHandler<PaymentFailedIntegrationEvent>
{
    private readonly IOrderWorkflow _orders;
    private readonly INotificationDispatcher _notifications;
    public PaymentFailedNotificationHandler(IOrderWorkflow orders, INotificationDispatcher notifications)
    {
        _orders = orders;
        _notifications = notifications;
    }

    public async Task HandleAsync(PaymentFailedIntegrationEvent e, CancellationToken ct = default)
    {
        var order = await _orders.GetPayableAsync(e.OrderId, ct);
        if (order is null) return;
        await _notifications.DispatchAsync(new NotificationRequest(
            order.CustomerId, NotificationType.PaymentFailed, e.EventId.ToString(),
            new Dictionary<string, string> { ["reason"] = e.Reason ?? string.Empty }), ct);
    }
}

public sealed class ShipmentCreatedNotificationHandler : IIntegrationEventHandler<ShipmentCreatedIntegrationEvent>
{
    private readonly IOrderFulfillment _orders;
    private readonly INotificationDispatcher _notifications;
    public ShipmentCreatedNotificationHandler(IOrderFulfillment orders, INotificationDispatcher notifications)
    {
        _orders = orders;
        _notifications = notifications;
    }

    public async Task HandleAsync(ShipmentCreatedIntegrationEvent e, CancellationToken ct = default)
    {
        var order = await _orders.GetFulfillmentAsync(e.OrderId, ct);
        if (order is null) return;
        await _notifications.DispatchAsync(new NotificationRequest(
            order.CustomerId, NotificationType.OrderShipped, e.EventId.ToString(),
            new Dictionary<string, string> { ["trackingNumber"] = e.TrackingNumber }), ct);
    }
}

public sealed class ShipmentDeliveredNotificationHandler : IIntegrationEventHandler<ShipmentDeliveredIntegrationEvent>
{
    private readonly IOrderFulfillment _orders;
    private readonly INotificationDispatcher _notifications;
    public ShipmentDeliveredNotificationHandler(IOrderFulfillment orders, INotificationDispatcher notifications)
    {
        _orders = orders;
        _notifications = notifications;
    }

    public async Task HandleAsync(ShipmentDeliveredIntegrationEvent e, CancellationToken ct = default)
    {
        var order = await _orders.GetFulfillmentAsync(e.OrderId, ct);
        if (order is null) return;
        await _notifications.DispatchAsync(new NotificationRequest(
            order.CustomerId, NotificationType.OrderDelivered, e.EventId.ToString(),
            new Dictionary<string, string> { ["trackingNumber"] = e.TrackingNumber }), ct);
    }
}
