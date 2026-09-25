using Npgsql;
using Xunit;

namespace CommerceHub.IntegrationTests;

/// <summary>
/// A <see cref="FactAttribute"/> that runs only when a PostgreSQL server is reachable on the dev port,
/// and skips otherwise. This encodes the project's "build now, verify under Docker later" discipline: the
/// DB-backed journeys are real, compiled, and ready — they light up automatically once the compose stack
/// (with migrations applied) is running, and stay green-by-skip in an infra-less environment.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!PostgresProbe.IsAvailable)
            Skip = "Requires a live PostgreSQL (docker-compose) with migrations applied. " +
                   "Deferred to the Docker-up verification pass.";
    }
}

/// <summary>
/// One-shot probe that opens a real connection to the project's dev database and runs a trivial query,
/// evaluated once per test run. A merely-open TCP port is not enough — the credentials and database must
/// actually be usable — so this connects with Npgsql rather than checking the socket.
/// </summary>
internal static class PostgresProbe
{
    // Matches appsettings.Development.json / docker-compose; short timeouts so an absent DB fails fast.
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=commercehub;Username=commercehub;Password=commercehub;" +
        "Timeout=2;Command Timeout=2";

    public static bool IsAvailable { get; } = Probe();

    private static bool Probe()
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();
            using var command = new NpgsqlCommand("SELECT 1", connection);
            command.ExecuteScalar();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
