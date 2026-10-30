using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Notifications.Application.Abstractions;
using Catrashop.Modules.Notifications.Contracts;
using Catrashop.Modules.Notifications.Domain;
using Microsoft.Extensions.Logging;

namespace Catrashop.Modules.Notifications.Application.Services;

/// <summary>
/// Implements the public <see cref="INotificationDispatcher"/> contract. Resolves the recipient's channel
/// preferences, renders the template, and delivers on each allowed channel (in-app row + email). Idempotent on
/// the request's key: a dispatch already recorded for that key is a no-op success, so a redelivered trigger never
/// notifies twice. A failed email does not fail the dispatch — the in-app notification still lands and the
/// outcome is recorded per channel.
/// </summary>
public sealed class NotificationDispatchService : INotificationDispatcher
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly INotificationDispatchRepository _dispatches;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly IEmailSender _email;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly ILogger<NotificationDispatchService> _logger;

    public NotificationDispatchService(
        INotificationRepository notifications,
        INotificationPreferenceRepository preferences,
        INotificationDispatchRepository dispatches,
        INotificationTemplateRenderer renderer,
        IEmailSender email,
        INotificationsUnitOfWork unitOfWork,
        TimeProvider clock,
        ILogger<NotificationDispatchService> logger)
    {
        _notifications = notifications;
        _preferences = preferences;
        _dispatches = dispatches;
        _renderer = renderer;
        _email = email;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result> DispatchAsync(NotificationRequest request, CancellationToken ct = default)
    {
        // Idempotency: this trigger may already have been delivered (e.g. a redelivered event).
        if (await _dispatches.ExistsAsync(request.IdempotencyKey, ct))
            return Result.Success();

        var preference = await _preferences.GetAsync(request.RecipientId, ct);
        bool Allowed(NotificationChannel channel) =>
            preference?.IsChannelAllowed(request.Type, channel) ?? true;

        var rendered = _renderer.Render(request.Type, request.Data);
        var now = _clock.GetUtcNow();

        var deliveredInApp = false;
        if (Allowed(NotificationChannel.InApp))
        {
            _notifications.Add(Notification.Create(request.RecipientId, request.Type, rendered.Title, rendered.Body, now));
            deliveredInApp = true;
        }

        var deliveredEmail = false;
        if (Allowed(NotificationChannel.Email) && !string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            var sent = await _email.SendAsync(
                new EmailMessage(request.RecipientEmail!, request.RecipientName, rendered.Title, rendered.Body), ct);
            deliveredEmail = sent.IsSuccess;
            if (sent.IsFailure)
                _logger.LogWarning("Email delivery failed for {Type} to recipient {RecipientId}: {Error}",
                    request.Type, request.RecipientId, sent.Error.Code);
        }

        _dispatches.Add(new NotificationDispatch(
            Guid.NewGuid(), request.IdempotencyKey, request.RecipientId, request.Type, deliveredInApp, deliveredEmail, now));

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
