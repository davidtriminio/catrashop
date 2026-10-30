using System.Diagnostics.Metrics;
using Catrashop.BuildingBlocks.Observability;

namespace Catrashop.Infrastructure.Observability;

/// <summary>
/// Metric instruments for the messaging pipeline, created on the shared <see cref="CatrashopTelemetry.Meter"/>
/// so they surface through the platform's OpenTelemetry exporters (OTLP / Prometheus) with no extra wiring.
/// </summary>
internal static class MessagingMetrics
{
    /// <summary>Count of integration events successfully published from an outbox to the broker, tagged by event type.</summary>
    public static readonly Counter<long> OutboxPublished = CatrashopTelemetry.Meter.CreateCounter<long>(
        "catrashop.messaging.outbox.published",
        unit: "{message}",
        description: "Integration events published from a module outbox to the broker.");

    /// <summary>Count of outbox publish failures (broker unavailable, serialization, etc.), tagged by event type.</summary>
    public static readonly Counter<long> OutboxFailed = CatrashopTelemetry.Meter.CreateCounter<long>(
        "catrashop.messaging.outbox.failed",
        unit: "{message}",
        description: "Outbox publish attempts that failed and will be retried.");
}
