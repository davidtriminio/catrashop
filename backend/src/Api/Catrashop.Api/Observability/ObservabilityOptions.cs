namespace Catrashop.Api.Observability;

/// <summary>
/// Observability wiring, bound from the "Observability" configuration section. Exporters are opt-in so the
/// platform stays self-contained and resilient with no collector present: OTLP is enabled only when an endpoint
/// is configured, and the console exporter is a dev convenience. The Prometheus scrape endpoint is in-process
/// (no external dependency), so it is on by default.
/// </summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>Logical service name stamped on the OpenTelemetry resource (traces/metrics/logs).</summary>
    public string ServiceName { get; set; } = "catrashop-api";

    /// <summary>OTLP collector endpoint (e.g. http://localhost:4317). When null/empty, OTLP export is disabled.</summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>Also export traces/metrics/logs to the console (useful in development).</summary>
    public bool ConsoleExporter { get; set; }

    /// <summary>Expose the in-process Prometheus scrape endpoint at <c>/metrics</c>.</summary>
    public bool PrometheusEndpoint { get; set; } = true;

    public bool OtlpEnabled => !string.IsNullOrWhiteSpace(OtlpEndpoint);
}
