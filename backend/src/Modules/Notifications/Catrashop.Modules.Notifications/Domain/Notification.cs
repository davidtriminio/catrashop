using System.Text.Json;
using Catrashop.BuildingBlocks.Domain;
using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Notifications.Contracts;

namespace Catrashop.Modules.Notifications.Domain;

/// <summary>
/// An in-app notification shown to one recipient in their inbox. Created when a dispatch is delivered on the
/// in-app channel; the recipient can mark it read. Content (title/body) is captured at delivery time so it never
/// changes retroactively.
/// </summary>
public sealed class Notification : AggregateRoot<Guid>
{
    private Notification() { } // EF

    private Notification(Guid id, Guid recipientId, NotificationType type, string title, string body, string? dataJson, DateTimeOffset nowUtc)
        : base(id)
    {
        RecipientId = recipientId;
        Type = type;
        Title = title;
        Body = body;
        DataJson = dataJson;
        IsRead = false;
        CreatedAtUtc = nowUtc;
    }

    public Guid RecipientId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    /// <summary>
    /// The data tokens the text was rendered from (JSON). With the type, they let the text be rendered again in
    /// the reader's language. Null on notifications created before this was stored.
    /// </summary>
    public string? DataJson { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static Notification Create(
        Guid recipientId, NotificationType type, string title, string body, DateTimeOffset nowUtc,
        IReadOnlyDictionary<string, string>? data = null) =>
        new(Guid.NewGuid(), recipientId, type, title, body,
            data is null ? null : JsonSerializer.Serialize(data), nowUtc);

    /// <summary>The stored data tokens, or null when this notification predates them.</summary>
    public IReadOnlyDictionary<string, string>? ReadData() =>
        DataJson is null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(DataJson);

    public Result MarkRead(DateTimeOffset nowUtc)
    {
        if (IsRead)
            return Result.Success(); // idempotent

        IsRead = true;
        ReadAtUtc = nowUtc;
        return Result.Success();
    }
}
