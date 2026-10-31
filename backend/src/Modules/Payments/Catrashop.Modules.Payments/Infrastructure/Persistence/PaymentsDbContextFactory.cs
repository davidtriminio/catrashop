using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catrashop.Modules.Payments.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> migrations (no live connection is opened).</summary>
public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("CATRASHOP_CONNECTION") ?? "Host=localhost;Database=catrashop;Username=catrashop;Password=placeholder",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "payments"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PaymentsDbContext(options);
    }
}
