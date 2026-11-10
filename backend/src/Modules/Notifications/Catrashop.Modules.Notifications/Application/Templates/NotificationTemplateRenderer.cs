using Catrashop.Modules.Notifications.Application.Abstractions;
using Catrashop.Modules.Notifications.Contracts;

namespace Catrashop.Modules.Notifications.Application.Templates;

/// <summary>
/// Renders each notification type into a title and body from its data tokens, in Spanish (the store is
/// Spanish-first, and the default) or English. Templates are kept in code (a switch) — a full template engine would
/// be over-engineering at this stage; missing tokens degrade gracefully to a readable default rather than throwing.
/// Notifications store the type and tokens, not the text, so the language is chosen when they are read.
/// </summary>
public sealed class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    public const string Spanish = "es";
    public const string English = "en";

    /// <summary>Maps an Accept-Language value (e.g. "en-US,en;q=0.9") to a supported language, defaulting to Spanish.</summary>
    public static string ResolveLanguage(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage)) return Spanish;
        var first = acceptLanguage.Split(',', 2)[0].Trim();
        return first.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? English : Spanish;
    }

    public RenderedNotification Render(NotificationType type, IReadOnlyDictionary<string, string> data, string language = Spanish)
    {
        var en = language == English;
        string Token(string key, string fallback = "") => data.TryGetValue(key, out var v) ? v : fallback;
        RenderedNotification T(string esTitle, string esBody, string enTitle, string enBody) =>
            en ? new(enTitle, enBody) : new(esTitle, esBody);

        var order = Token("orderNumber");

        return type switch
        {
            NotificationType.Welcome => T(
                "Bienvenido a Catrashop",
                $"Hola {Token("name", "")}, gracias por crear tu cuenta. ¡Felices compras!".Replace("Hola ,", "Hola,"),
                "Welcome to Catrashop",
                $"Hi {Token("name", "there")}, thanks for creating your account. Happy shopping!"),

            NotificationType.PasswordReset => T(
                "Restablece tu contraseña",
                "Recibimos una solicitud para restablecer tu contraseña. Si fuiste tú, sigue el enlace que te enviamos para continuar.",
                "Reset your password",
                "We received a request to reset your password. If this was you, follow the link we sent to continue."),

            NotificationType.OrderPlaced => T(
                "Recibimos tu pedido",
                $"Tu pedido {order} fue registrado y está pendiente de pago.",
                "We received your order",
                $"Your order {order} has been placed and is awaiting payment."),

            NotificationType.OrderPaid => T(
                "Tu pedido está confirmado",
                $"Recibimos el pago del pedido {order}. Total: {Token("total")} {Token("currency")}.",
                "Your order is confirmed",
                $"Payment for order {order} was received. Total: {Token("total")} {Token("currency")}."),

            NotificationType.PaymentFailed => T(
                "No se pudo procesar el pago",
                $"No pudimos procesar el pago del pedido {order}. Puedes intentarlo de nuevo desde tus pedidos.",
                "Payment could not be processed",
                $"We couldn't process payment for order {order}. You can try again from your orders."),

            NotificationType.OrderShipped => T(
                "Tu pedido va en camino",
                $"El pedido {order} fue enviado. Rastréalo con el número {Token("trackingNumber")}.",
                "Your order is on its way",
                $"Order {order} has shipped. Track it with {Token("trackingNumber")}."),

            NotificationType.OrderDelivered => T(
                "Tu pedido fue entregado",
                $"El pedido {order} fue entregado. ¡Esperamos que lo disfrutes, y tu reseña nos ayudaría mucho!",
                "Your order was delivered",
                $"Order {order} was delivered. We hope you love it — a review would be appreciated!"),

            NotificationType.OrderCancelled => T(
                "Tu pedido fue cancelado",
                $"El pedido {order} fue cancelado. {Token("reason")}".TrimEnd(),
                "Your order was cancelled",
                $"Order {order} has been cancelled. {Token("reason")}".TrimEnd()),

            NotificationType.RefundCompleted => T(
                "Tu reembolso está completo",
                $"Se procesó el reembolso del pedido {order}.",
                "Your refund is complete",
                $"A refund for order {order} has been processed."),

            NotificationType.BackInStock => T(
                "Ya hay existencias",
                $"{Token("productName", "Un producto de tus favoritos")} está disponible de nuevo.",
                "It's back in stock",
                $"{Token("productName", "An item on your wishlist")} is available again."),

            // Promotions carry text written by an admin: it is shown as written, in any language.
            NotificationType.Promotion => T(
                Token("title", "Una nueva oferta para ti"),
                Token("message", "Descubre las últimas ofertas en Catrashop."),
                Token("title", "A new offer for you"),
                Token("message", "Check out the latest deals at Catrashop.")),

            _ => T(
                "Notificación", Token("message", "Tienes una nueva notificación."),
                "Notification", Token("message", "You have a new notification."))
        };
    }
}
