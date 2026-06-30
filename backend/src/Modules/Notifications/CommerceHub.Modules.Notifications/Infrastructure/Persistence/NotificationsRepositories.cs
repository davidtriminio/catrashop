using CommerceHub.BuildingBlocks.Pagination;
using CommerceHub.Modules.Notifications.Application.Abstractions;
using CommerceHub.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _context;
    public NotificationRepository(NotificationsDbContext context) => _context = context;

    public void Add(Notification notification) => _context.Notifications.Add(notification);

    public Task<Notification?> GetAsync(Guid id, Guid recipientId, CancellationToken ct = default) =>
        _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == recipientId, ct);

    public async Task<PagedResult<Notification>> ListForRecipientAsync(
        Guid recipientId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default)
    {
        var request = new PageRequest(page, pageSize);
        var query = _context.Notifications.Where(n => n.RecipientId == recipientId);
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Notification>(items, request.Page, request.PageSize, total);
    }

    public Task<int> UnreadCountAsync(Guid recipientId, CancellationToken ct = default) =>
        _context.Notifications.CountAsync(n => n.RecipientId == recipientId && !n.IsRead, ct);

    public async Task<int> MarkAllReadAsync(Guid recipientId, DateTimeOffset nowUtc, CancellationToken ct = default)
    {
        var unread = await _context.Notifications
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var notification in unread)
            notification.MarkRead(nowUtc);

        return unread.Count;
    }
}

public sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly NotificationsDbContext _context;
    public NotificationPreferenceRepository(NotificationsDbContext context) => _context = context;

    public void Add(NotificationPreference preference) => _context.Preferences.Add(preference);

    // Mutes are an owned collection and load with the preference automatically.
    public Task<NotificationPreference?> GetAsync(Guid customerId, CancellationToken ct = default) =>
        _context.Preferences.FirstOrDefaultAsync(p => p.Id == customerId, ct);
}

public sealed class NotificationDispatchRepository : INotificationDispatchRepository
{
    private readonly NotificationsDbContext _context;
    public NotificationDispatchRepository(NotificationsDbContext context) => _context = context;

    public void Add(NotificationDispatch dispatch) => _context.Dispatches.Add(dispatch);

    public Task<bool> ExistsAsync(string idempotencyKey, CancellationToken ct = default) =>
        _context.Dispatches.AnyAsync(d => d.IdempotencyKey == idempotencyKey, ct);
}
