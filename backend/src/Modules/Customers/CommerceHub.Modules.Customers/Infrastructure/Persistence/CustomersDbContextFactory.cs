using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommerceHub.Modules.Customers.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations (no live connection is opened).</summary>
public sealed class CustomersDbContextFactory : IDesignTimeDbContextFactory<CustomersDbContext>
{
    public CustomersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CustomersDbContext>()
            .UseNpgsql("Host=localhost;Database=commercehub;Username=commercehub;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "customers"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CustomersDbContext(options);
    }
}
