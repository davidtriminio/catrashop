using CommerceHub.BuildingBlocks.Pagination;
using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Notifications.Application.Abstractions;
using CommerceHub.Modules.Notifications.Application.Contracts;
using CommerceHub.Modules.Notifications.Domain;

namespace CommerceHub.Modules.Notifications.Application.Services;

/// <summary>The recipient's in-app inbox: list, unread count, and mark-read (all scoped to the caller).</summary>
public sealed class NotificationQueryService
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public NotificationQueryService(INotificationRepository notifications, INotificationsUnitOfWork unitOfWork, TimeProvider clock)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<PagedResult<NotificationResponse>> ListAsync(Guid recipientId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _notifications.ListForRecipientAsync(recipientId, unreadOnly, page, pageSize, ct);
        return new PagedResult<NotificationResponse>(
            result.Items.Select(NotificationMappers.ToResponse).ToArray(), result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<int> UnreadCountAsync(Guid recipientId, CancellationToken ct = default) =>
        await _notifications.UnreadCountAsync(recipientId, ct);

    public async Task<Result> MarkReadAsync(Guid recipientId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _notifications.GetAsync(notificationId, recipientId, ct);
        if (notification is null)
            return NotificationErrors.NotFound;

        notification.MarkRead(_clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<int> MarkAllReadAsync(Guid recipientId, CancellationToken ct = default)
    {
        var affected = await _notifications.MarkAllReadAsync(recipientId, _clock.GetUtcNow(), ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return affected;
    }
}
