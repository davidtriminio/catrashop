using CommerceHub.Modules.Notifications.Contracts;

namespace CommerceHub.Modules.Notifications.Application.Contracts;

// --- Requests -----------------------------------------------------------------------------------

/// <summary>Admin/manual send. IdempotencyKey is optional — a fresh one is generated when omitted.</summary>
public sealed record SendNotificationRequest(
    Guid RecipientId,
    NotificationType Type,
    Dictionary<string, string>? Data,
    string? RecipientEmail,
    string? RecipientName,
    string? IdempotencyKey);

public sealed record UpdateChannelPreferenceRequest(NotificationType Type, NotificationChannel Channel, bool Muted);

// --- Responses ----------------------------------------------------------------------------------

public sealed record NotificationResponse(
    Guid Id, string Type, string Title, string Body, bool IsRead, DateTimeOffset CreatedAtUtc, DateTimeOffset? ReadAtUtc);

public sealed record UnreadCountResponse(int Unread);

public sealed record MutedChannelResponse(string Type, string Channel);

public sealed record NotificationPreferenceResponse(IReadOnlyList<MutedChannelResponse> Muted);
