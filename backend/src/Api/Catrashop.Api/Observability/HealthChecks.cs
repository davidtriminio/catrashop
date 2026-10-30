using Catrashop.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using RabbitMQ.Client;

namespace Catrashop.Api.Observability;

/// <summary>
/// Readiness check for PostgreSQL: opens a connection and runs <c>SELECT 1</c> with a short timeout. Tagged
/// "ready" so it gates <c>/health/ready</c> (503 when the database is down) but never <c>/health/live</c> — the
/// process stays alive and the app keeps serving infra-independent endpoints, matching the platform's resilience.
/// </summary>
public sealed class NpgsqlHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public NpgsqlHealthCheck(IConfiguration configuration) =>
        _connectionString = configuration.GetConnectionString("Postgres") ?? string.Empty;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return HealthCheckResult.Unhealthy("No 'Postgres' connection string is configured.");

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString) { Timeout = 3, CommandTimeout = 3 };
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is not reachable.", ex);
        }
    }
}

/// <summary>
/// Readiness check for RabbitMQ: opens a short-lived connection with the configured credentials. Tagged "ready"
/// only, for the same reason as the database check — a broker outage must not fail liveness.
/// </summary>
public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;

    public RabbitMqHealthCheck(IOptions<RabbitMqOptions> options) => _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3),
            };
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ is reachable.")
                : HealthCheckResult.Unhealthy("RabbitMQ connection is not open.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is not reachable.", ex);
        }
    }
}
