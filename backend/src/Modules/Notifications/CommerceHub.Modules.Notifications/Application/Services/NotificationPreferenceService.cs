using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Notifications.Application.Abstractions;
using CommerceHub.Modules.Notifications.Application.Contracts;
using CommerceHub.Modules.Notifications.Domain;

namespace CommerceHub.Modules.Notifications.Application.Services;

/// <summary>A customer's notification preferences: read the muted set and mute/unmute a (type, channel).</summary>
public sealed class NotificationPreferenceService
{
    private readonly INotificationPreferenceRepository _preferences;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public NotificationPreferenceService(INotificationPreferenceRepository preferences, INotificationsUnitOfWork unitOfWork, TimeProvider clock)
    {
        _preferences = preferences;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<NotificationPreferenceResponse> GetAsync(Guid customerId, CancellationToken ct = default) =>
        NotificationMappers.ToResponse(await _preferences.GetAsync(customerId, ct));

    public async Task<Result<NotificationPreferenceResponse>> UpdateAsync(
        Guid customerId, UpdateChannelPreferenceRequest request, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var preference = await _preferences.GetAsync(customerId, ct);
        if (preference is null)
        {
            preference = NotificationPreference.Create(customerId, now);
            _preferences.Add(preference);
        }

        if (request.Muted)
        {
            var muted = preference.Mute(request.Type, request.Channel, now);
            if (muted.IsFailure)
                return muted.Error;
        }
        else
        {
            preference.Unmute(request.Type, request.Channel, now);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return NotificationMappers.ToResponse(preference);
    }
}
