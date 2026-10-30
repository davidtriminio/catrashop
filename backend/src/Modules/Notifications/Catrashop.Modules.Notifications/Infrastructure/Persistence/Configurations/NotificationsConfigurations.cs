using Catrashop.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catrashop.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(40);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(2000).IsRequired();

        // Inbox queries are per recipient, newest first, often filtered to unread.
        builder.HasIndex(n => new { n.RecipientId, n.IsRead }).HasDatabaseName("ix_notifications_recipient_read");
    }
}

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");
        builder.HasKey(p => p.Id); // Id == customer id (1:1)

        builder.OwnsMany(p => p.Mutes, m =>
        {
            m.ToTable("notification_mutes");
            m.WithOwner().HasForeignKey(x => x.PreferenceId);
            m.HasKey(x => x.Id);
            m.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            m.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
            m.HasIndex(x => new { x.PreferenceId, x.Type, x.Channel }).IsUnique().HasDatabaseName("ux_notification_mutes_topic");
        });

        builder.Metadata.FindNavigation(nameof(NotificationPreference.Mutes))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class NotificationDispatchConfiguration : IEntityTypeConfiguration<NotificationDispatch>
{
    public void Configure(EntityTypeBuilder<NotificationDispatch> builder)
    {
        builder.ToTable("notification_dispatches");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(d => d.IdempotencyKey).IsUnique().HasDatabaseName("ux_notification_dispatches_key");
        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(40);
    }
}
