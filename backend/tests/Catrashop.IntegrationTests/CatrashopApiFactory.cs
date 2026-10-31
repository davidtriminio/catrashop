using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Catrashop.IntegrationTests;

/// <summary>
/// Boots the real API host in-memory over the full HTTP pipeline (authentication, authorization,
/// routing, exception handling). The host is deliberately configured <b>without live infrastructure</b>:
/// a placeholder PostgreSQL connection string satisfies module registration (EF connects lazily, so the
/// app still starts), and the resilient messaging background services tolerate an absent broker. These
/// tests therefore exercise everything that does not require a database round-trip; DB-backed journeys
/// are deferred to the Docker-up verification pass.
/// </summary>
public sealed class CatrashopApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Signing key shared with <see cref="TestTokens"/> so minted JWTs validate against the host.</summary>
    public const string JwtSigningKey = "integration-tests-signing-key-0123456789";
    public const string JwtIssuer = "catrashop";
    public const string JwtAudience = "catrashop-clients";

    public CatrashopApiFactory()
    {
        // The minimal-hosting ConfigurationManager materializes before WebApplicationFactory's
        // ConfigureAppConfiguration delegates run, so an in-memory source cannot reliably override the
        // app's own appsettings. Environment variables are added after the JSON sources by the default
        // builder and win deterministically — set them here, before the host boots on first CreateClient.
        Environment.SetEnvironmentVariable("Identity__Jwt__SigningKey", JwtSigningKey);
        Environment.SetEnvironmentVariable("Identity__Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Identity__Jwt__Audience", JwtAudience);
        // The PostgreSQL connection string is left to appsettings.Development.json (the docker-compose dev
        // database). Pipeline tests never open a connection; the DB-backed E2E journey uses it under Docker.
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");
}
