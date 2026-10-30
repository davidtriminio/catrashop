using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Catrashop.Api.Security;

/// <summary>
/// Composition-root wiring for Phase 20 security hardening: response headers, CORS, per-client rate limiting
/// (with a tighter budget on credential endpoints), optional HTTPS/HSTS, and a fail-fast guard that refuses to
/// boot outside Development with placeholder secrets. Every control is config-driven with safe defaults, so the
/// app still runs locally and the integration host (Development) is unaffected.
/// </summary>
public static class SecurityHardeningExtensions
{
    public const string CorsPolicyName = "catrashop";
    public const string GeneralLimiterPolicy = "general";
    public const string AuthLimiterPolicy = "auth";

    /// <summary>Route prefixes whose unauthenticated endpoints get the tighter auth rate-limit budget.</summary>
    private static readonly string[] AuthPathPrefixes =
    [
        "/api/v1/identity/login",
        "/api/v1/identity/register",
        "/api/v1/identity/refresh",
        "/api/v1/identity/password",
    ];

    public static WebApplicationBuilder AddCatrashopSecurity(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>()
                      ?? new SecurityOptions();
        builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));

        // Fail fast on insecure configuration — before the host starts accepting traffic.
        SecretGuard.Validate(builder.Environment, builder.Configuration);

        // Never advertise the server implementation.
        builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

        // Behind Apache/Cloudflare: trust X-Forwarded-* from the local reverse proxy (loopback) so rate limiting
        // and HTTPS detection see the real client.
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        ConfigureCors(builder.Services, options.Cors);
        ConfigureRateLimiting(builder.Services, options.RateLimiting);

        return builder;
    }

    private static void ConfigureCors(IServiceCollection services, SecurityOptions.CorsOptions cors)
    {
        services.AddCors(o => o.AddPolicy(CorsPolicyName, policy =>
        {
            if (cors.AllowedOrigins.Length == 0)
            {
                // No browser origin is trusted explicitly: emit no CORS headers (same-origin only).
                policy.DisallowCredentials();
                return;
            }

            policy
                .WithOrigins(cors.AllowedOrigins)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithHeaders("Authorization", "Content-Type", CorrelationIdHeader)
                .WithExposedHeaders(CorrelationIdHeader)
                .AllowCredentials();
        }));
    }

    private const string CorrelationIdHeader = "X-Correlation-ID";

    private static void ConfigureRateLimiting(IServiceCollection services, SecurityOptions.RateLimitingOptions rl)
    {
        if (!rl.Enabled) return;

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // One partition per client (authenticated user id when present, else remote IP), with a tighter
            // window for the credential endpoints. The limiter is global so no module needs to opt in.
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var isAuthPath = AuthPathPrefixes.Any(p =>
                    context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
                var key = ClientKey(context);
                var permit = isAuthPath ? rl.AuthPermitPerWindow : rl.PermitPerWindow;
                var prefix = isAuthPath ? "auth:" : "gen:";

                return RateLimitPartition.GetFixedWindowLimiter(prefix + key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, permit),
                    Window = TimeSpan.FromSeconds(Math.Max(1, rl.WindowSeconds)),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = Math.Max(0, rl.QueueLimit),
                });
            });

            limiter.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Rate limit exceeded. Slow down and retry after the indicated delay.",
                }), token);
            };
        });
    }

    /// <summary>Partition key: the authenticated subject when available, otherwise the client IP.</summary>
    private static string ClientKey(HttpContext context)
    {
        var sub = context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(sub)) return "u:" + sub;
        return "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }

    /// <summary>
    /// Inserts the security middleware into the pipeline. Call order matters: headers first (so they apply to
    /// every response including rejections), then HTTPS/HSTS, CORS, and the rate limiter.
    /// </summary>
    public static WebApplication UseCatrashopSecurity(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<SecurityOptions>>().Value;

        app.UseForwardedHeaders();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (options.EnableHttpsRedirection)
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors(CorsPolicyName);

        if (options.RateLimiting.Enabled)
            app.UseRateLimiter();

        return app;
    }
}
