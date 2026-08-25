using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CommerceHub.BuildingBlocks.Observability;

/// <summary>
/// The platform's own telemetry sources. Any layer can emit spans on <see cref="ActivitySource"/> and metrics on
/// <see cref="Meter"/> using these names; the API composition root subscribes OpenTelemetry to them by name
/// (<see cref="ActivitySourceName"/> / <see cref="MeterName"/>), so instrumentation stays decoupled from the
/// exporter wiring. Living in BuildingBlocks lets every module and the infrastructure share one source without
/// referencing the host.
/// </summary>
public static class CommerceHubTelemetry
{
    public const string ActivitySourceName = "CommerceHub";
    public const string MeterName = "CommerceHub";

    /// <summary>The platform-wide semantic version stamped on the telemetry resource and sources.</summary>
    public const string Version = "0.2.0";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);
    public static readonly Meter Meter = new(MeterName, Version);
}
