using CommerceHub.Modules.Notifications.Application.Contracts;
using CommerceHub.Modules.Notifications.Domain;

namespace CommerceHub.Modules.Notifications.Application.Services;

internal static class NotificationMappers
{
    public static NotificationResponse ToResponse(Notification n) =>
        new(n.Id, n.Type.ToString(), n.Title, n.Body, n.IsRead, n.CreatedAtUtc, n.ReadAtUtc);

    public static NotificationPreferenceResponse ToResponse(NotificationPreference? preference)
    {
        var muted = preference is null
            ? Array.Empty<MutedChannelResponse>()
            : preference.Mutes.Select(m => new MutedChannelResponse(m.Type.ToString(), m.Channel.ToString())).ToArray();
        return new NotificationPreferenceResponse(muted);
    }
}
