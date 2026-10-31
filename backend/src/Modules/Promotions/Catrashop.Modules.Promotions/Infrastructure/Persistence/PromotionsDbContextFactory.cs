using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catrashop.Modules.Promotions.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations (no live connection is opened).</summary>
public sealed class PromotionsDbContextFactory : IDesignTimeDbContextFactory<PromotionsDbContext>
{
    public PromotionsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PromotionsDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("CATRASHOP_CONNECTION") ?? "Host=localhost;Database=catrashop;Username=catrashop;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "promotions"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PromotionsDbContext(options);
    }
}
