using CommerceHub.BuildingBlocks.Domain;
using CommerceHub.Modules.Notifications.Contracts;

namespace CommerceHub.Modules.Notifications.Domain;

/// <summary>
/// The record of one dispatch, keyed by the caller's <see cref="IdempotencyKey"/> (unique). Its existence is the
/// idempotency guard: a redelivered trigger with the same key is a no-op. It also captures which channels the
/// dispatch actually delivered on, for audit.
/// </summary>
public sealed class NotificationDispatch : Entity<Guid>
{
    private NotificationDispatch() { } // EF

    public NotificationDispatch(Guid id, string idempotencyKey, Guid recipientId, NotificationType type,
        bool deliveredInApp, bool deliveredEmail, DateTimeOffset nowUtc) : base(id)
    {
        IdempotencyKey = idempotencyKey;
        RecipientId = recipientId;
        Type = type;
        DeliveredInApp = deliveredInApp;
        DeliveredEmail = deliveredEmail;
        CreatedAtUtc = nowUtc;
    }

    public string IdempotencyKey { get; private set; } = default!;
    public Guid RecipientId { get; private set; }
    public NotificationType Type { get; private set; }
    public bool DeliveredInApp { get; private set; }
    public bool DeliveredEmail { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
