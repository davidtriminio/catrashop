using Catrashop.Modules.Notifications.Application.Abstractions;
using Catrashop.Modules.Notifications.Contracts;

namespace Catrashop.Modules.Notifications.Application.Templates;

/// <summary>
/// Renders each notification type into a Spanish title and body (the store is Spanish-first) from its data tokens. Templates are kept in code (a
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
                "Bienvenido a Catrashop",
                $"Hola {Token("name", "")}, gracias por crear tu cuenta. ¡Felices compras!".Replace("Hola ,", "Hola,")),

            NotificationType.PasswordReset => new(
                "Restablece tu contraseña",
                "Recibimos una solicitud para restablecer tu contraseña. Si fuiste tú, sigue el enlace que te enviamos para continuar."),

            NotificationType.OrderPlaced => new(
                "Recibimos tu pedido",
                $"Tu pedido {Token("orderNumber")} fue registrado y está pendiente de pago."),

            NotificationType.OrderPaid => new(
                "Tu pedido está confirmado",
                $"Recibimos el pago del pedido {Token("orderNumber")}. Total: {Token("total")} {Token("currency")}."),

            NotificationType.PaymentFailed => new(
                "No se pudo procesar el pago",
                $"No pudimos procesar el pago del pedido {Token("orderNumber")}. Puedes intentarlo de nuevo desde tus pedidos."),

            NotificationType.OrderShipped => new(
                "Tu pedido va en camino",
                $"El pedido {Token("orderNumber")} fue enviado. Rastréalo con el número {Token("trackingNumber")}."),

            NotificationType.OrderDelivered => new(
                "Tu pedido fue entregado",
                $"El pedido {Token("orderNumber")} fue entregado. ¡Esperamos que lo disfrutes, y tu reseña nos ayudaría mucho!"),

            NotificationType.OrderCancelled => new(
                "Tu pedido fue cancelado",
                $"El pedido {Token("orderNumber")} fue cancelado. {Token("reason")}".TrimEnd()),

            NotificationType.RefundCompleted => new(
                "Tu reembolso está completo",
                $"Se procesó el reembolso del pedido {Token("orderNumber")}."),

            NotificationType.BackInStock => new(
                "Ya hay existencias",
                $"{Token("productName", "Un producto de tus favoritos")} está disponible de nuevo."),

            NotificationType.Promotion => new(
                Token("title", "Una nueva oferta para ti"),
                Token("message", "Descubre las últimas ofertas en Catrashop.")),

            _ => new("Notificación", Token("message", "Tienes una nueva notificación."))
        };
    }
}
