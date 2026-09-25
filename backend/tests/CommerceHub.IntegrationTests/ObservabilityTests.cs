using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace CommerceHub.IntegrationTests;

/// <summary>
/// Observability surface: every response carries a correlation id (generated or echoed from the request),
/// and the in-process Prometheus scrape endpoint serves metrics in the exposition format.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ObservabilityTests
{
    private const string CorrelationHeader = "X-Correlation-ID";

    private readonly CommerceHubApiFactory _factory;

    public ObservabilityTests(CommerceHubApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Every_response_carries_a_correlation_id_header()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.True(response.Headers.Contains(CorrelationHeader));
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.GetValues(CorrelationHeader).First()));
    }

    [Fact]
    public async Task An_inbound_correlation_id_is_echoed_back()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationHeader, "trace-abc-123");

        var response = await client.SendAsync(request);

        Assert.Equal("trace-abc-123", response.Headers.GetValues(CorrelationHeader).First());
    }

    [Fact]
    public async Task Prometheus_scrape_endpoint_serves_metrics()
    {
        var client = _factory.CreateClient();

        // Exercise a request first so ASP.NET Core instrumentation has something to report.
        await client.GetAsync("/");

        var response = await client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        // OpenMetrics/Prometheus exposition format uses "# HELP"/"# TYPE" comment lines.
        Assert.Contains("# TYPE", body);
    }
}
