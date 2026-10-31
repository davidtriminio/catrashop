using Microsoft.Extensions.Options;

namespace Catrashop.Api.Security;

/// <summary>
/// Adds defence-in-depth response headers on every response. These are cheap, standards-based hardening
/// controls (clickjacking, MIME-sniffing, referrer leakage, cross-origin isolation) that apply equally to a
/// JSON API — a misrouted HTML response or an embedded error page still inherits them. The <c>Server</c>
/// banner is suppressed at the Kestrel level (see <see cref="SecurityHardeningExtensions"/>).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions.SecurityHeaderOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityOptions> options)
    {
        _next = next;
        _options = options.Value.Headers;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // Headers are mutable until the response body starts; middleware runs before any endpoint writes.
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";

        if (!string.IsNullOrWhiteSpace(_options.ContentSecurityPolicy))
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;

        return _next(context);
    }
}
