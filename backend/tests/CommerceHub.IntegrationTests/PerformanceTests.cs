using CommerceHub.Api.Performance;
using CommerceHub.BuildingBlocks.Caching;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CommerceHub.IntegrationTests;

/// <summary>
/// Phase 22 performance controls on the live pipeline: JSON responses compress when the client negotiates an
/// encoding (and are left untouched otherwise), and the output-cache services are wired for the anonymous
/// catalog reads. A cache HIT round-trip needs the catalog database and is verified under Docker.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class PerformanceTests
{
    private readonly CommerceHubApiFactory _factory;

    public PerformanceTests(CommerceHubApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("gzip")]
    [InlineData("br")]
    public async Task Json_response_is_compressed_when_the_client_negotiates_it(string encoding)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", encoding);

        var response = await client.SendAsync(request);

        Assert.Contains(encoding, response.Content.Headers.ContentEncoding);
    }

    [Fact]
    public async Task Response_is_not_compressed_when_the_client_does_not_ask()
    {
        var client = _factory.CreateClient();

        // No Accept-Encoding header: the middleware leaves the body alone, so existing tests that read the
        // response body as text keep working.
        var response = await client.GetAsync("/");

        Assert.Empty(response.Content.Headers.ContentEncoding);
    }

    [Fact]
    public void Output_cache_is_registered_for_the_catalog_read_policy()
    {
        // The store singleton exists only when AddOutputCache ran; the catalog endpoints tag themselves with
        // the shared policy name (kept in BuildingBlocks so no module references the API host).
        Assert.NotNull(_factory.Services.GetService<IOutputCacheStore>());
        Assert.Equal("catalog-reads", OutputCachePolicies.CatalogReads);
    }

    [Fact]
    public void Performance_options_bind_with_safe_defaults()
    {
        var options = _factory.Services.GetRequiredService<IOptions<PerformanceOptions>>().Value;

        Assert.True(options.ResponseCompression.Enabled);
        Assert.False(options.ResponseCompression.EnableForHttps); // BREACH-safe default
        Assert.True(options.OutputCache.Enabled);
        Assert.True(options.OutputCache.CatalogTtlSeconds > 0);
    }
}
