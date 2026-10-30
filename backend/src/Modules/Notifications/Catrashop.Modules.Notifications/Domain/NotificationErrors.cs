using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Notifications.Contracts;

namespace Catrashop.Modules.Notifications.Domain;

public static class NotificationErrors
{
    public static readonly Error NotFound =
        Error.NotFound("notifications.not_found", "Notification not found.");

    public static Error TypeNotMutable(NotificationType type) =>
        Error.Validation("notifications.type_not_mutable",
            $"'{type}' is a transactional notification and cannot be muted.");
}
