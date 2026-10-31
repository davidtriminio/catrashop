using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catrashop.Modules.Cart.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations (no live connection is opened).</summary>
public sealed class CartDbContextFactory : IDesignTimeDbContextFactory<CartDbContext>
{
    public CartDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CartDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("CATRASHOP_CONNECTION") ?? "Host=localhost;Database=catrashop;Username=catrashop;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "cart"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CartDbContext(options);
    }
}
