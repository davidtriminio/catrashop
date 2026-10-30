using Catrashop.Modules.Notifications.Contracts;

namespace Catrashop.Modules.Notifications.Domain;

/// <summary>Static metadata about each notification type: its category, which decides whether it can be muted.</summary>
public static class NotificationCatalog
{
    /// <summary>
    /// Marketing types a customer may opt out of. Everything else is transactional (order, payment, account
    /// events) and is always delivered on every channel — a customer cannot silence a receipt or a shipment update.
    /// </summary>
    private static readonly HashSet<NotificationType> Marketing = new()
    {
        NotificationType.Promotion,
        NotificationType.BackInStock
    };

    public static bool IsMarketing(NotificationType type) => Marketing.Contains(type);

    /// <summary>Transactional notifications cannot be muted; marketing ones can.</summary>
    public static bool IsMutable(NotificationType type) => Marketing.Contains(type);
}
