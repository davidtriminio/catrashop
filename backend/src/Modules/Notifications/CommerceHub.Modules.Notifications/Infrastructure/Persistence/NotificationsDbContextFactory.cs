using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommerceHub.Modules.Notifications.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations (no live connection is opened).</summary>
public sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("COMMERCEHUB_CONNECTION") ?? "Host=localhost;Database=commercehub;Username=commercehub;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "notifications"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new NotificationsDbContext(options);
    }
}
