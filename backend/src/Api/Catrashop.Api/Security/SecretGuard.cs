using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Catrashop.Api.Security;

/// <summary>
/// Refuses to start the host with development placeholder secrets in any non-Development environment. The
/// sample values checked in to <c>appsettings.Development.json</c> are convenient locally but catastrophic in
/// production (a known JWT signing key forges any identity; a known webhook secret forges payment callbacks),
/// so we fail loudly at boot rather than silently trusting them. Development keeps the placeholders — this is
/// how the app runs offline — so the guard is a no-op there.
/// </summary>
public static class SecretGuard
{
    /// <summary>Placeholder fragments that must never reach a real environment.</summary>
    private static readonly string[] ForbiddenFragments =
    [
        "change-me",
        "dev-only",
        "0123456789",
    ];

    public static void Validate(IHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsDevelopment()) return;

        var violations = new List<string>();

        Check(violations, "Identity:Jwt:SigningKey", configuration["Identity:Jwt:SigningKey"], minLength: 32);
        Check(violations, "Payments:WebhookSecret", configuration["Payments:WebhookSecret"], minLength: 16);

        if (violations.Count > 0)
            throw new InvalidOperationException(
                $"Insecure configuration for environment '{environment.EnvironmentName}': " +
                string.Join("; ", violations) +
                ". Provide strong secrets via environment variables (e.g. Identity__Jwt__SigningKey).");
    }

    private static void Check(List<string> violations, string key, string? value, int minLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            violations.Add($"'{key}' is not set");
            return;
        }

        if (value.Length < minLength)
            violations.Add($"'{key}' is shorter than the required {minLength} characters");

        foreach (var fragment in ForbiddenFragments)
        {
            if (value.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"'{key}' still contains the development placeholder '{fragment}'");
                break;
            }
        }
    }
}
