using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Notifications.Contracts;

/// <summary>The kinds of notification the platform can send. Each has a category (see the domain catalog) that
/// decides whether a customer may mute it.</summary>
public enum NotificationType
{
    // Account (transactional)
    Welcome = 0,
    PasswordReset = 1,

    // Orders / payments (transactional)
    OrderPlaced = 10,
    OrderPaid = 11,
    PaymentFailed = 12,
    OrderShipped = 13,
    OrderDelivered = 14,
    OrderCancelled = 15,
    RefundCompleted = 16,

    // Marketing (mutable)
    Promotion = 50,
    BackInStock = 51
}

/// <summary>A channel a notification can be delivered on.</summary>
public enum NotificationChannel
{
    InApp = 0,
    Email = 1
}

/// <summary>
/// Public, in-process contract for sending a notification. Other modules (and, from Phase 15, the messaging
/// consumers that react to integration events) call this; Notifications resolves the recipient's channel
/// preferences and delivers on each allowed channel. Idempotent on <see cref="NotificationRequest.IdempotencyKey"/>,
/// so a redelivered trigger never notifies twice.
/// </summary>
public interface INotificationDispatcher
{
    Task<Result> DispatchAsync(NotificationRequest request, CancellationToken ct = default);
}

/// <summary>
/// A request to notify one recipient. <see cref="RecipientEmail"/> is optional — email is only attempted when it
/// is present and the channel is allowed — so Notifications never has to reach into Identity/Customers for it.
/// <see cref="Data"/> carries the template tokens for the type (e.g. orderNumber, total).
/// </summary>
public sealed record NotificationRequest(
    Guid RecipientId,
    NotificationType Type,
    string IdempotencyKey,
    IReadOnlyDictionary<string, string> Data,
    string? RecipientEmail = null,
    string? RecipientName = null);
