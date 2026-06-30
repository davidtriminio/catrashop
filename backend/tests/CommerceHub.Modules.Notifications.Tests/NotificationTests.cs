using CommerceHub.Modules.Notifications.Application.Templates;
using CommerceHub.Modules.Notifications.Contracts;
using CommerceHub.Modules.Notifications.Domain;
using Xunit;

namespace CommerceHub.Modules.Notifications.Tests;

public class NotificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    // --- Catalog -------------------------------------------------------------------------------
    [Theory]
    [InlineData(NotificationType.Promotion, true)]
    [InlineData(NotificationType.BackInStock, true)]
    [InlineData(NotificationType.OrderPaid, false)]
    [InlineData(NotificationType.PasswordReset, false)]
    [InlineData(NotificationType.OrderDelivered, false)]
    public void Only_marketing_types_are_mutable(NotificationType type, bool mutable) =>
        Assert.Equal(mutable, NotificationCatalog.IsMutable(type));

    // --- In-app notification -------------------------------------------------------------------
    [Fact]
    public void New_notification_is_unread()
    {
        var n = Notification.Create(Guid.NewGuid(), NotificationType.OrderPaid, "t", "b", Now);

        Assert.False(n.IsRead);
        Assert.Null(n.ReadAtUtc);
    }

    [Fact]
    public void Marking_read_sets_the_timestamp_and_is_idempotent()
    {
        var n = Notification.Create(Guid.NewGuid(), NotificationType.OrderPaid, "t", "b", Now);

        Assert.True(n.MarkRead(Now).IsSuccess);
        Assert.True(n.IsRead);
        Assert.Equal(Now, n.ReadAtUtc);

        Assert.True(n.MarkRead(Now.AddDays(1)).IsSuccess); // idempotent
        Assert.Equal(Now, n.ReadAtUtc); // timestamp unchanged
    }

    // --- Preferences ---------------------------------------------------------------------------
    [Fact]
    public void Muting_a_marketing_type_blocks_only_that_channel()
    {
        var prefs = NotificationPreference.Create(Guid.NewGuid(), Now);

        Assert.True(prefs.Mute(NotificationType.Promotion, NotificationChannel.Email, Now).IsSuccess);

        Assert.False(prefs.IsChannelAllowed(NotificationType.Promotion, NotificationChannel.Email));
        Assert.True(prefs.IsChannelAllowed(NotificationType.Promotion, NotificationChannel.InApp)); // other channel free
    }

    [Fact]
    public void Transactional_types_cannot_be_muted()
    {
        var prefs = NotificationPreference.Create(Guid.NewGuid(), Now);

        var result = prefs.Mute(NotificationType.OrderPaid, NotificationChannel.Email, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("notifications.type_not_mutable", result.Error.Code);
    }

    [Fact]
    public void Transactional_types_are_always_allowed_even_though_they_cannot_be_muted()
    {
        var prefs = NotificationPreference.Create(Guid.NewGuid(), Now);

        Assert.True(prefs.IsChannelAllowed(NotificationType.OrderPaid, NotificationChannel.Email));
        Assert.True(prefs.IsChannelAllowed(NotificationType.OrderPaid, NotificationChannel.InApp));
    }

    [Fact]
    public void Muting_is_deduplicated_and_unmuting_restores_the_channel()
    {
        var prefs = NotificationPreference.Create(Guid.NewGuid(), Now);

        prefs.Mute(NotificationType.Promotion, NotificationChannel.Email, Now);
        prefs.Mute(NotificationType.Promotion, NotificationChannel.Email, Now); // duplicate
        Assert.Single(prefs.Mutes);

        prefs.Unmute(NotificationType.Promotion, NotificationChannel.Email, Now);
        Assert.Empty(prefs.Mutes);
        Assert.True(prefs.IsChannelAllowed(NotificationType.Promotion, NotificationChannel.Email));
    }

    // --- Template rendering --------------------------------------------------------------------
    [Fact]
    public void Order_paid_template_fills_its_tokens()
    {
        var renderer = new NotificationTemplateRenderer();

        var rendered = renderer.Render(NotificationType.OrderPaid, new Dictionary<string, string>
        {
            ["orderNumber"] = "ORD-1", ["total"] = "42.00", ["currency"] = "USD"
        });

        Assert.Contains("ORD-1", rendered.Body);
        Assert.Contains("42.00", rendered.Body);
        Assert.Contains("USD", rendered.Body);
        Assert.False(string.IsNullOrWhiteSpace(rendered.Title));
    }

    [Fact]
    public void Template_degrades_gracefully_when_a_token_is_missing()
    {
        var renderer = new NotificationTemplateRenderer();

        var rendered = renderer.Render(NotificationType.OrderShipped, new Dictionary<string, string>());

        Assert.False(string.IsNullOrWhiteSpace(rendered.Title));
        Assert.False(string.IsNullOrWhiteSpace(rendered.Body)); // no throw, still readable
    }

    [Fact]
    public void Promotion_template_uses_caller_supplied_title_and_message()
    {
        var renderer = new NotificationTemplateRenderer();

        var rendered = renderer.Render(NotificationType.Promotion, new Dictionary<string, string>
        {
            ["title"] = "Summer Sale", ["message"] = "50% off everything"
        });

        Assert.Equal("Summer Sale", rendered.Title);
        Assert.Equal("50% off everything", rendered.Body);
    }
}
