using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Catrashop.IntegrationTests;

/// <summary>
/// The per-client rate limiter shields the unauthenticated credential endpoints from brute force. This host is
/// booted with a deliberately tiny auth budget (3 per window) so the limiter trips deterministically: a burst
/// of login attempts from the one test client exhausts the budget and the surplus is answered with 429 and a
/// <c>Retry-After</c> header, without any database involvement.
/// </summary>
public sealed class RateLimitingTests : IClassFixture<RateLimitingTests.Factory>
{
    private readonly Factory _factory;

    public RateLimitingTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task A_burst_of_login_attempts_is_eventually_rate_limited()
    {
        var client = _factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 12; i++)
        {
            var response = await client.PostAsync("/api/v1/identity/login",
                new StringContent("""{"email":"a@b.test","password":"wrong-password"}""", Encoding.UTF8, "application/json"));
            statuses.Add(response.StatusCode);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Assert.True(response.Headers.Contains("Retry-After"));
                Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
            }
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }

    /// <summary>Host with a 3-per-window auth budget, isolated from the shared integration fixture.</summary>
    public sealed class Factory : WebApplicationFactory<Program>
    {
        public Factory()
        {
            Environment.SetEnvironmentVariable("Identity__Jwt__SigningKey", CatrashopApiFactory.JwtSigningKey);
            Environment.SetEnvironmentVariable("Security__RateLimiting__AuthPermitPerWindow", "3");
            Environment.SetEnvironmentVariable("Security__RateLimiting__WindowSeconds", "60");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");
    }
}
