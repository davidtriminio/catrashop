using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommerceHub.Modules.Shipping.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations (no live connection is opened).</summary>
public sealed class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    public ShippingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("COMMERCEHUB_CONNECTION") ?? "Host=localhost;Database=commercehub;Username=commercehub;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "shipping"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ShippingDbContext(options);
    }
}
