using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace CommerceHub.IntegrationTests;

/// <summary>
/// Phase 20 hardening on the live pipeline: every response carries the defence-in-depth headers and never
/// leaks a <c>Server</c> banner, and CORS honours only the configured SPA origins (dev config allows
/// <c>http://localhost:4200</c> / <c>:4300</c>) while rejecting an unknown origin.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SecurityHeadersAndCorsTests
{
    private readonly CommerceHubApiFactory _factory;

    public SecurityHeadersAndCorsTests(CommerceHubApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Responses_carry_the_security_headers()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal("nosniff", First(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", First(response, "X-Frame-Options"));
        Assert.Equal("no-referrer", First(response, "Referrer-Policy"));
        Assert.Equal("same-origin", First(response, "Cross-Origin-Opener-Policy"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task Server_banner_is_not_advertised()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task Cors_preflight_from_an_allowed_origin_is_permitted()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/identity/login");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        Assert.Equal("http://localhost:4200", First(response, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Cors_preflight_from_an_unknown_origin_gets_no_allow_header()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/identity/login");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static string? First(HttpResponseMessage response, string header) =>
        response.Headers.TryGetValues(header, out var values) ? values.FirstOrDefault() : null;
}
