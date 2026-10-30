using CommerceHub.Modules.Notifications.Application.Abstractions;
using CommerceHub.Modules.Notifications.Contracts;

namespace CommerceHub.Modules.Notifications.Application.Templates;

/// <summary>
/// Renders each notification type into a title and body from its data tokens. Templates are kept in code (a
/// switch) — a full template engine would be over-engineering at this stage; missing tokens degrade gracefully
/// to a readable default rather than throwing.
/// </summary>
public sealed class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    public RenderedNotification Render(NotificationType type, IReadOnlyDictionary<string, string> data)
    {
        string Token(string key, string fallback = "") => data.TryGetValue(key, out var v) ? v : fallback;

        return type switch
        {
            NotificationType.Welcome => new(
                "Welcome to Catrashop",
                $"Hi {Token("name", "there")}, thanks for creating your account. Happy shopping!"),

            NotificationType.PasswordReset => new(
                "Reset your password",
                "We received a request to reset your password. If this was you, follow the link we sent to continue."),

            NotificationType.OrderPlaced => new(
                "We received your order",
                $"Your order {Token("orderNumber")} has been placed and is awaiting payment."),

            NotificationType.OrderPaid => new(
                "Your order is confirmed",
                $"Payment for order {Token("orderNumber")} was received. Total: {Token("total")} {Token("currency")}."),

            NotificationType.PaymentFailed => new(
                "Payment could not be processed",
                $"We couldn't process payment for order {Token("orderNumber")}. You can try again from your orders."),

            NotificationType.OrderShipped => new(
                "Your order is on its way",
                $"Order {Token("orderNumber")} has shipped. Track it with {Token("trackingNumber")}."),

            NotificationType.OrderDelivered => new(
                "Your order was delivered",
                $"Order {Token("orderNumber")} was delivered. We hope you love it — a review would be appreciated!"),

            NotificationType.OrderCancelled => new(
                "Your order was cancelled",
                $"Order {Token("orderNumber")} has been cancelled. {Token("reason")}".TrimEnd()),

            NotificationType.RefundCompleted => new(
                "Your refund is complete",
                $"A refund for order {Token("orderNumber")} has been processed."),

            NotificationType.BackInStock => new(
                "It's back in stock",
                $"{Token("productName", "An item on your wishlist")} is available again."),

            NotificationType.Promotion => new(
                Token("title", "A new offer for you"),
                Token("message", "Check out the latest deals at Catrashop.")),

            _ => new("Notification", Token("message", "You have a new notification."))
        };
    }
}
