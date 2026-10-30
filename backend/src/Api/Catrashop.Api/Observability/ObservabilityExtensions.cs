using System.Text.Json;
using Catrashop.BuildingBlocks.Observability;
using Catrashop.Infrastructure.Messaging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Catrashop.Api.Observability;

/// <summary>
/// Composition-root wiring for the three observability pillars — structured logs, distributed traces, and
/// metrics — plus health probes. Exporters are configuration-driven and opt-in, so the app runs self-contained
/// with none present and lights up a collector (OTLP) when one is configured.
/// </summary>
public static class ObservabilityExtensions
{
    public const string LiveTag = "live";
    public const string ReadyTag = "ready";

    public static WebApplicationBuilder AddCatrashopObservability(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
                      ?? new ObservabilityOptions();
        builder.Services.Configure<ObservabilityOptions>(builder.Configuration.GetSection(ObservabilityOptions.SectionName));

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName: options.ServiceName, serviceVersion: CatrashopTelemetry.Version);

        ConfigureLogging(builder, options, resource);
        ConfigureTracingAndMetrics(builder, options, resource);
        ConfigureHealthChecks(builder.Services, builder.Configuration);

        return builder;
    }

    private static void ConfigureLogging(WebApplicationBuilder builder, ObservabilityOptions options, ResourceBuilder resource)
    {
        // Own the provider list so logs are structured and carry trace/span ids (and the request correlation scope).
        builder.Logging.ClearProviders();
        builder.Logging.Configure(o =>
            o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);

        if (builder.Environment.IsDevelopment())
            builder.Logging.AddSimpleConsole(o => { o.IncludeScopes = true; o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
        else
            builder.Logging.AddJsonConsole(o => { o.IncludeScopes = true; });

        builder.Logging.AddOpenTelemetry(o =>
        {
            o.SetResourceBuilder(resource);
            o.IncludeScopes = true;
            o.IncludeFormattedMessage = true;
            if (options.OtlpEnabled) o.AddOtlpExporter(x => x.Endpoint = new Uri(options.OtlpEndpoint!));
        });
    }

    private static void ConfigureTracingAndMetrics(WebApplicationBuilder builder, ObservabilityOptions options, ResourceBuilder resource)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName: options.ServiceName, serviceVersion: CatrashopTelemetry.Version))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                    .AddHttpClientInstrumentation()
                    .AddSource(CatrashopTelemetry.ActivitySourceName)
                    .AddSource("Npgsql");

                if (options.ConsoleExporter) tracing.AddConsoleExporter();
                if (options.OtlpEnabled) tracing.AddOtlpExporter(x => x.Endpoint = new Uri(options.OtlpEndpoint!));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(CatrashopTelemetry.MeterName)
                    .AddMeter("Npgsql");

                if (options.PrometheusEndpoint) metrics.AddPrometheusExporter();
                if (options.ConsoleExporter) metrics.AddConsoleExporter();
                if (options.OtlpEnabled) metrics.AddOtlpExporter(x => x.Endpoint = new Uri(options.OtlpEndpoint!));
            });
    }

    private static void ConfigureHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        var checks = services.AddHealthChecks()
            .AddCheck<NpgsqlHealthCheck>("postgres", failureStatus: HealthStatus.Unhealthy, tags: [ReadyTag]);

        // No broker to probe when events are dispatched in-process.
        if (!MessagingRegistration.UsesInProcessTransport(configuration))
            checks.AddCheck<RabbitMqHealthCheck>("rabbitmq", failureStatus: HealthStatus.Unhealthy, tags: [ReadyTag]);
    }

    /// <summary>Maps the liveness/readiness probes and (optionally) the Prometheus scrape endpoint.</summary>
    public static WebApplication MapCatrashopObservability(this WebApplication app)
    {
        // Liveness: no infra dependency — healthy whenever the process can serve requests.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(LiveTag),
            ResponseWriter = WriteHealthResponse,
        });

        // Readiness: gated on infrastructure (Postgres, RabbitMQ) — 503 while any dependency is down.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = WriteHealthResponse,
        });

        var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservabilityOptions>>().Value;
        if (options.PrometheusEndpoint) app.MapPrometheusScrapingEndpoint();

        return app;
    }

    private static Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
            }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
