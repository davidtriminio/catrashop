using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommerceHub.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> to build the model for migrations. The connection
/// string here is only used to construct the provider; no database connection is opened to scaffold
/// migrations, so a placeholder is fine and keeps secrets out of tooling.
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=commercehub;Username=commercehub;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new IdentityDbContext(options);
    }
}
