namespace Catrashop.Api.Security;

/// <summary>
/// Host-level security hardening knobs (config section <c>Security</c>). Everything is opt-out-able and has
/// safe defaults, so the app runs locally with no configuration while production can tighten each control.
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public CorsOptions Cors { get; set; } = new();
    public RateLimitingOptions RateLimiting { get; set; } = new();
    public SecurityHeaderOptions Headers { get; set; } = new();

    /// <summary>
    /// Redirect HTTP → HTTPS and emit HSTS. Off by default because the dev host and the in-memory
    /// integration host serve plain HTTP; a TLS-terminating environment turns it on.
    /// </summary>
    public bool EnableHttpsRedirection { get; set; }

    public sealed class CorsOptions
    {
        /// <summary>
        /// Exact origins allowed to call the API from a browser. Empty means the browser same-origin policy
        /// is the only gate (no CORS headers emitted) — correct when the SPA is served from the API's origin
        /// or behind a reverse proxy. Wildcards are never honoured; each origin is matched verbatim.
        /// </summary>
        public string[] AllowedOrigins { get; set; } = [];
    }

    public sealed class RateLimitingOptions
    {
        public bool Enabled { get; set; } = true;

        /// <summary>Requests per window allowed for a client on ordinary endpoints.</summary>
        public int PermitPerWindow { get; set; } = 120;

        /// <summary>Window length in seconds for the general limiter.</summary>
        public int WindowSeconds { get; set; } = 60;

        /// <summary>
        /// Tighter budget for unauthenticated credential endpoints (login/register/refresh/password),
        /// which are the brute-force and enumeration surface.
        /// </summary>
        public int AuthPermitPerWindow { get; set; } = 10;

        /// <summary>Requests queued once the budget is exhausted before a 429 is returned.</summary>
        public int QueueLimit { get; set; }
    }

    public sealed class SecurityHeaderOptions
    {
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Content-Security-Policy sent on responses. The default locks an API down completely (it serves
        /// JSON, never HTML/scripts); a host that also serves a page can relax it.
        /// </summary>
        public string ContentSecurityPolicy { get; set; } =
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
    }
}
