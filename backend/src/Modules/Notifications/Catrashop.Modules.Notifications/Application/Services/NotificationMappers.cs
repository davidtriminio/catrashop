using Catrashop.Modules.Notifications.Application.Abstractions;
using Catrashop.Modules.Notifications.Application.Contracts;
using Catrashop.Modules.Notifications.Application.Templates;
using Catrashop.Modules.Notifications.Domain;

namespace Catrashop.Modules.Notifications.Application.Services;

internal static class NotificationMappers
{
    /// <summary>
    /// Shows a notification in the reader's language by rendering its type + data again. Notifications saved before
    /// the data was stored are recovered from their text; if that fails the text saved at delivery is shown as is.
    /// </summary>
    public static NotificationResponse ToResponse(Notification n, INotificationTemplateRenderer renderer, string language)
    {
        var data = n.ReadData() ?? LegacyNotificationData.TryExtract(n.Type, n.Body);
        var (title, body) = data is null ? (n.Title, n.Body) : Unpack(renderer.Render(n.Type, data, language));
        return new(n.Id, n.Type.ToString(), title, body, n.IsRead, n.CreatedAtUtc, n.ReadAtUtc);
    }

    private static (string Title, string Body) Unpack(RenderedNotification r) => (r.Title, r.Body);

    public static NotificationPreferenceResponse ToResponse(NotificationPreference? preference)
    {
        var muted = preference is null
            ? Array.Empty<MutedChannelResponse>()
            : preference.Mutes.Select(m => new MutedChannelResponse(m.Type.ToString(), m.Channel.ToString())).ToArray();
        return new NotificationPreferenceResponse(muted);
    }
}
