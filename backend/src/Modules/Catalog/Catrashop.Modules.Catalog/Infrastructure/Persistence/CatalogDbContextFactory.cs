using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catrashop.Modules.Catalog.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations (no live connection is opened).</summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("CATRASHOP_CONNECTION") ?? "Host=localhost;Database=catrashop;Username=catrashop;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "catalog"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CatalogDbContext(options);
    }
}
