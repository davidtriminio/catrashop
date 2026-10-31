using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Notifications.Application.Abstractions;
using Catrashop.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Notifications.Infrastructure.Persistence;

/// <summary>EF Core context for the Notifications module. Owns the "notifications" PostgreSQL schema.</summary>
public sealed class NotificationsDbContext : ModuleDbContext, INotificationsUnitOfWork
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    protected override string Schema => "notifications";

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> Preferences => Set<NotificationPreference>();
    public DbSet<NotificationDispatch> Dispatches => Set<NotificationDispatch>();
}
