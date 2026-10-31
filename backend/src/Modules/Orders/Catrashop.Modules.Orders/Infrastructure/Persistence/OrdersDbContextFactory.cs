using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catrashop.Modules.Orders.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations (no live connection is opened).</summary>
public sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("CATRASHOP_CONNECTION") ?? "Host=localhost;Database=catrashop;Username=catrashop;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "orders"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new OrdersDbContext(options);
    }
}
