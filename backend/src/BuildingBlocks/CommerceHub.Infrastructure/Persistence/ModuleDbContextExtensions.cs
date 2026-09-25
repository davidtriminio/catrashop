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
    /// <remarks>
    /// Uses <c>AddDbContextPool</c> (Phase 22 performance review): the context is reset and reused between
    /// requests instead of being constructed from scratch each time, which removes per-request context and
    /// internal-service-provider allocation on the hot request path. Every module context is pooling-safe —
    /// its constructor takes only <see cref="Microsoft.EntityFrameworkCore.DbContextOptions"/> and it holds no
    /// request-scoped state — so the default pool of 1024 is left in place.
    /// </remarks>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        string schema)
        where TContext : ModuleDbContext
    {
        services.AddDbContextPool<TContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", schema))
                .UseSnakeCaseNamingConvention());

        return services;
    }
}
