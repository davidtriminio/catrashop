using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommerceHub.Modules.Inventory.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations (no live connection is opened).</summary>
public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("COMMERCEHUB_CONNECTION") ?? "Host=localhost;Database=commercehub;Username=commercehub;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "inventory"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new InventoryDbContext(options);
    }
}
