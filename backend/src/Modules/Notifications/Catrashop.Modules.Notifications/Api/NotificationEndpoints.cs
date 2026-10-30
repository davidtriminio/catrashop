using Catrashop.BuildingBlocks.Results;
using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.Notifications.Application.Contracts;
using Catrashop.Modules.Notifications.Application.Services;
using Catrashop.Modules.Notifications.Contracts;
using Catrashop.Modules.Notifications.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catrashop.Modules.Notifications.Api;

internal static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").WithTags("Notifications");

        // --- Customer inbox (me-scoped) ----------------------------------------------------------
        group.MapGet("/", async (
            ICurrentUser user, NotificationQueryService query,
            bool? unreadOnly, int page, int pageSize, CancellationToken ct) =>
        {
            if (user.UserId is not { } recipientId) return Results.Unauthorized();
            return Results.Ok(await query.ListAsync(recipientId, unreadOnly ?? false, page, pageSize, ct));
        })
        .RequireAuthorization()
        .WithSummary("List my in-app notifications (newest first).");

        group.MapGet("/unread-count", async (ICurrentUser user, NotificationQueryService query, CancellationToken ct) =>
        {
            if (user.UserId is not { } recipientId) return Results.Unauthorized();
            return Results.Ok(new UnreadCountResponse(await query.UnreadCountAsync(recipientId, ct)));
        })
        .RequireAuthorization()
        .WithSummary("Count my unread notifications.");

        group.MapPost("/{id:guid}/read", async (Guid id, ICurrentUser user, NotificationQueryService query, CancellationToken ct) =>
        {
            if (user.UserId is not { } recipientId) return Results.Unauthorized();
            return (await query.MarkReadAsync(recipientId, id, ct)).ToHttpResult();
        })
        .RequireAuthorization()
        .WithSummary("Mark one notification read.");

        group.MapPost("/read-all", async (ICurrentUser user, NotificationQueryService query, CancellationToken ct) =>
        {
            if (user.UserId is not { } recipientId) return Results.Unauthorized();
            return Results.Ok(new { updated = await query.MarkAllReadAsync(recipientId, ct) });
        })
        .RequireAuthorization()
        .WithSummary("Mark all my notifications read.");

        // --- Customer preferences (me-scoped) ----------------------------------------------------
        group.MapGet("/preferences", async (ICurrentUser user, NotificationPreferenceService prefs, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return Results.Ok(await prefs.GetAsync(customerId, ct));
        })
        .RequireAuthorization()
        .WithSummary("Get my notification preferences (muted channels).");

        group.MapPut("/preferences", async (
            UpdateChannelPreferenceRequest request, IValidator<UpdateChannelPreferenceRequest> validator,
            ICurrentUser user, NotificationPreferenceService prefs, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            if (await Validate(validator, request) is { } problem) return problem;
            return (await prefs.UpdateAsync(customerId, request, ct)).ToHttpResult();
        })
        .RequireAuthorization()
        .WithSummary("Mute or unmute a marketing notification on a channel.");

        // --- Admin / operations (notifications.manage) -------------------------------------------
        var admin = app.MapGroup("/api/v1/notifications/admin").WithTags("Notifications (admin)")
            .RequirePermission(NotificationPermissions.Manage);

        admin.MapPost("/send", async (
            SendNotificationRequest request, IValidator<SendNotificationRequest> validator,
            INotificationDispatcher dispatcher, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;

            var dispatch = new NotificationRequest(
                request.RecipientId,
                request.Type,
                string.IsNullOrWhiteSpace(request.IdempotencyKey) ? $"manual:{Guid.NewGuid():N}" : request.IdempotencyKey,
                request.Data ?? new Dictionary<string, string>(),
                request.RecipientEmail,
                request.RecipientName);

            return (await dispatcher.DispatchAsync(dispatch, ct)).ToHttpResult();
        })
        .WithSummary("Send a notification to a recipient (manual / operational).");

        return app;
    }

    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
