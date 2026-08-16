using System.Net;
using System.Text.Json;
using Xunit;

namespace CommerceHub.IntegrationTests;

/// <summary>
/// The host-level endpoints that must respond without any infrastructure: liveness/readiness probes,
/// the service banner (which enumerates the composed modules), and the 404 path for unknown routes.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class HealthAndRootTests
{
    private readonly CommerceHubApiFactory _factory;

    public HealthAndRootTests(CommerceHubApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_probes_report_healthy(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Root_lists_every_composed_module()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("CommerceHub API", root.GetProperty("service").GetString());
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
