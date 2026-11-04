using Catrashop.Modules.Notifications.Application.Services;
using Catrashop.Modules.Notifications.Application.Templates;
using Catrashop.Modules.Notifications.Contracts;
using Catrashop.Modules.Notifications.Domain;
using Xunit;

namespace Catrashop.Modules.Notifications.Tests;

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

    [Theory]
    [InlineData(null, "es")]
    [InlineData("", "es")]
    [InlineData("es-HN,es;q=0.9", "es")]
    [InlineData("en-US,en;q=0.9", "en")]
    [InlineData("fr", "es")]
    public void Language_is_resolved_from_accept_language_defaulting_to_spanish(string? header, string expected) =>
        Assert.Equal(expected, NotificationTemplateRenderer.ResolveLanguage(header));

    [Fact]
    public void Same_data_renders_in_either_language()
    {
        var renderer = new NotificationTemplateRenderer();
        var data = new Dictionary<string, string> { ["orderNumber"] = "ORD-7" };

        var es = renderer.Render(NotificationType.OrderPlaced, data, "es");
        var en = renderer.Render(NotificationType.OrderPlaced, data, "en");

        Assert.Equal("Recibimos tu pedido", es.Title);
        Assert.Equal("We received your order", en.Title);
        Assert.Contains("ORD-7", es.Body);
        Assert.Contains("ORD-7", en.Body);
    }

    [Fact]
    public void Notification_keeps_its_data_so_it_can_be_rendered_again()
    {
        var data = new Dictionary<string, string> { ["orderNumber"] = "ORD-9" };
        var n = Notification.Create(Guid.NewGuid(), NotificationType.OrderPlaced, "t", "b", Now, data);

        Assert.Equal("ORD-9", n.ReadData()!["orderNumber"]);
        Assert.Null(Notification.Create(Guid.NewGuid(), NotificationType.OrderPlaced, "t", "b", Now).ReadData());
    }

    [Theory]
    [InlineData(NotificationType.OrderPaid, "Payment for order ORD-1 was received. Total: 42.00 HNL.", "orderNumber", "ORD-1")]
    [InlineData(NotificationType.OrderShipped, "Order ORD-2 has shipped. Track it with TRK 99.", "trackingNumber", "TRK 99")]
    [InlineData(NotificationType.Welcome, "Hi Ana, thanks for creating your account. Happy shopping!", "name", "Ana")]
    [InlineData(NotificationType.BackInStock, "Blue Mug is available again.", "productName", "Blue Mug")]
    [InlineData(NotificationType.OrderCancelled, "Order ORD-3 has been cancelled. Out of stock", "reason", "Out of stock")]
    [InlineData(NotificationType.OrderPlaced, "Tu pedido ORD-4 fue registrado y está pendiente de pago.", "orderNumber", "ORD-4")]
    public void Old_notifications_get_their_data_back_from_their_text(NotificationType type, string body, string key, string value)
    {
        var data = LegacyNotificationData.TryExtract(type, body);

        Assert.NotNull(data);
        Assert.Equal(value, data![key]);
    }

    [Fact]
    public void Old_english_notification_is_shown_in_spanish()
    {
        var renderer = new NotificationTemplateRenderer();
        var n = Notification.Create(Guid.NewGuid(), NotificationType.OrderShipped, "Your order is on its way",
            "Order ORD-5 has shipped. Track it with ABC123.", Now);

        var response = NotificationMappers.ToResponse(n, renderer, "es");

        Assert.Equal("Tu pedido va en camino", response.Title);
        Assert.Contains("ORD-5", response.Body);
        Assert.Contains("ABC123", response.Body);
    }

    [Fact]
    public void Unrecognised_old_text_is_shown_as_stored()
    {
        var renderer = new NotificationTemplateRenderer();
        var n = Notification.Create(Guid.NewGuid(), NotificationType.Promotion, "Verano", "50% de descuento", Now);

        var response = NotificationMappers.ToResponse(n, renderer, "en");

        Assert.Equal("Verano", response.Title);
        Assert.Equal("50% de descuento", response.Body);
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
