using CommerceHub.BuildingBlocks.Domain;
using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Notifications.Contracts;

namespace CommerceHub.Modules.Notifications.Domain;

/// <summary>
/// One customer's notification preferences, keyed on their id (1:1). It holds the set of (type, channel) pairs
/// the customer has muted; a channel is allowed for a type unless it is both mutable (marketing) and muted.
/// Transactional notifications can never be muted, so <see cref="IsChannelAllowed"/> always permits them.
/// </summary>
public sealed class NotificationPreference : AggregateRoot<Guid>
{
    private readonly List<MutedChannel> _mutes = new();

    private NotificationPreference() { } // EF

    private NotificationPreference(Guid customerId, DateTimeOffset nowUtc) : base(customerId)
    {
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<MutedChannel> Mutes => _mutes.AsReadOnly();

    public static NotificationPreference Create(Guid customerId, DateTimeOffset nowUtc) => new(customerId, nowUtc);

    /// <summary>Mute a marketing notification type on a channel. Transactional types cannot be muted.</summary>
    public Result Mute(NotificationType type, NotificationChannel channel, DateTimeOffset nowUtc)
    {
        if (!NotificationCatalog.IsMutable(type))
            return NotificationErrors.TypeNotMutable(type);

        if (!_mutes.Any(m => m.Type == type && m.Channel == channel))
        {
            _mutes.Add(new MutedChannel(Guid.NewGuid(), Id, type, channel));
            UpdatedAtUtc = nowUtc;
        }
        return Result.Success();
    }

    public void Unmute(NotificationType type, NotificationChannel channel, DateTimeOffset nowUtc)
    {
        var removed = _mutes.RemoveAll(m => m.Type == type && m.Channel == channel);
        if (removed > 0)
            UpdatedAtUtc = nowUtc;
    }

    public bool IsChannelAllowed(NotificationType type, NotificationChannel channel)
    {
        if (!NotificationCatalog.IsMutable(type))
            return true; // transactional — always delivered
        return !_mutes.Any(m => m.Type == type && m.Channel == channel);
    }
}

/// <summary>A single muted (type, channel) pair owned by a <see cref="NotificationPreference"/>.</summary>
public sealed class MutedChannel : Entity<Guid>
{
    private MutedChannel() { } // EF

    internal MutedChannel(Guid id, Guid preferenceId, NotificationType type, NotificationChannel channel) : base(id)
    {
        PreferenceId = preferenceId;
        Type = type;
        Channel = channel;
    }

    public Guid PreferenceId { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
}
