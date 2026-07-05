using CommerceHub.BuildingBlocks.Pagination;
using CommerceHub.BuildingBlocks.Persistence;
using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Notifications.Application.Contracts;
using CommerceHub.Modules.Notifications.Contracts;
using CommerceHub.Modules.Notifications.Domain;

namespace CommerceHub.Modules.Notifications.Application.Abstractions;

/// <summary>The Notifications module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface INotificationsUnitOfWork : IUnitOfWork;

/// <summary>Sends an email through an external provider. The dev implementation logs instead of sending.</summary>
public interface IEmailSender
{
    Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(string ToEmail, string? ToName, string Subject, string Body);

/// <summary>Renders a notification type + its data tokens into the title/body shown to the recipient.</summary>
public interface INotificationTemplateRenderer
{
    RenderedNotification Render(NotificationType type, IReadOnlyDictionary<string, string> data);
}

public sealed record RenderedNotification(string Title, string Body);

public interface INotificationRepository
{
    void Add(Notification notification);
    Task<Notification?> GetAsync(Guid id, Guid recipientId, CancellationToken ct = default);
    Task<PagedResult<Notification>> ListForRecipientAsync(Guid recipientId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default);
    Task<int> UnreadCountAsync(Guid recipientId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(Guid recipientId, DateTimeOffset nowUtc, CancellationToken ct = default);
}

public interface INotificationPreferenceRepository
{
    void Add(NotificationPreference preference);
    Task<NotificationPreference?> GetAsync(Guid customerId, CancellationToken ct = default);
}

public interface INotificationDispatchRepository
{
    void Add(NotificationDispatch dispatch);
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken ct = default);
}
