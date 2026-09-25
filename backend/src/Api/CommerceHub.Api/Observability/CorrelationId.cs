using System.Diagnostics;

namespace CommerceHub.Api.Observability;

/// <summary>
/// Ensures every request has a correlation id: honours an inbound <c>X-Correlation-ID</c> header (so a caller or
/// upstream gateway can stitch a request across services) or falls back to the active trace id / a new GUID. The
/// id is pushed into a logging scope (so every log line for the request carries it), tagged on the current
/// Activity (so it appears on traces), and echoed back in the response header.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private const string LogPropertyName = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context);

        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("commercehub.correlation_id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object> { [LogPropertyName] = correlationId }))
        {
            await _next(context);
        }
    }

    private static string Resolve(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var header))
        {
            var value = header.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }

        return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }
}
