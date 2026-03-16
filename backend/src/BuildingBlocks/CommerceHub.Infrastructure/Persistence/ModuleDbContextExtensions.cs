using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceHub.Infrastructure.Persistence;

public static class ModuleDbContextExtensions
{
    /// <summary>
    /// Registers a module's <see cref="ModuleDbContext"/> against PostgreSQL with the platform-wide
    /// conventions: snake_case naming and a per-schema migrations-history table (so each module owns
    /// its migration state independently).
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        string schema)
        where TContext : ModuleDbContext
    {
        services.AddDbContext<TContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", schema))
                .UseSnakeCaseNamingConvention());

        return services;
    }
}
