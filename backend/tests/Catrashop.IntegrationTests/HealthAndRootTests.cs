using System.Net;
using System.Text.Json;
using Xunit;

namespace Catrashop.IntegrationTests;

/// <summary>
/// The host-level endpoints that must respond without any infrastructure: liveness/readiness probes,
/// the service banner (which enumerates the composed modules), and the 404 path for unknown routes.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class HealthAndRootTests
{
    private readonly CatrashopApiFactory _factory;

    public HealthAndRootTests(CatrashopApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Liveness_is_healthy_without_infrastructure()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_reports_its_infra_checks_and_is_unavailable_while_infra_is_down()
    {
        // No Docker: Postgres and RabbitMQ are unreachable, so readiness must be 503 (a load balancer would
        // pull the instance) while liveness stays healthy. The body enumerates each dependency check.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var names = doc.RootElement.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToArray();
        Assert.Contains("postgres", names);
        Assert.Contains("rabbitmq", names);
        Assert.Equal("Unhealthy", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Root_lists_every_composed_module()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("Catrashop API", root.GetProperty("service").GetString());
        Assert.Equal("ok", root.GetProperty("status").GetString());

        var modules = root.GetProperty("modules").EnumerateArray().Select(m => m.GetString()).ToArray();
        Assert.Equal(11, modules.Length);
        Assert.Contains("Catalog", modules);
        Assert.Contains("Identity", modules);
        Assert.Contains("AI", modules);
    }

    [Fact]
    public async Task Unknown_route_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
