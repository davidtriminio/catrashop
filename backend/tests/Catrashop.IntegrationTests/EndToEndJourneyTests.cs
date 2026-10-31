using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Catrashop.IntegrationTests;

/// <summary>
/// A DB-backed end-to-end slice through the Identity module: register a customer, then use the issued
/// access token to read the authenticated profile. It touches a real PostgreSQL (write + read), the JWT
/// issuance path, and the authenticated request pipeline end to end. Gated by <see cref="PostgresFactAttribute"/>,
/// so it runs only when the compose database is up (migrations applied) and skips cleanly otherwise.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class EndToEndJourneyTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CatrashopApiFactory _factory;

    public EndToEndJourneyTests(CatrashopApiFactory factory) => _factory = factory;

    [PostgresFact]
    public async Task Register_then_read_own_profile_with_the_issued_token()
    {
        var client = _factory.CreateClient();
        var email = $"e2e-{Guid.NewGuid():N}@catrashop.test";

        // 1) Register — the API persists the user and returns a fresh token pair.
        var register = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email,
            password = "Sup3r-Secret-Pw!",
            firstName = "Ada",
            lastName = "Lovelace",
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var tokens = await register.Content.ReadFromJsonAsync<AuthTokens>(Json);
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrWhiteSpace(tokens!.AccessToken));

        // 2) Call an authenticated endpoint with the issued access token.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var profile = await me.Content.ReadFromJsonAsync<Profile>(Json);
        Assert.NotNull(profile);
        Assert.Equal(email, profile!.Email);
    }

    [PostgresFact]
    public async Task Unregistered_login_is_rejected()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@catrashop.test",
            password = "whatever-Pw-1!",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [PostgresFact]
    public async Task Concurrent_refresh_with_the_same_token_does_not_revoke_the_session()
    {
        var client = _factory.CreateClient();
        var tokens = await RegisterAsync(client);

        // Two clients (tabs, or foreground + background task) refresh with the same single-use token.
        var first = await client.PostAsJsonAsync("/api/v1/identity/refresh", new { refreshToken = tokens.RefreshToken });
        var second = await client.PostAsJsonAsync("/api/v1/identity/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        // Both resulting sessions stay usable: neither refresh token was revoked by the other.
        var firstTokens = (await first.Content.ReadFromJsonAsync<AuthTokens>(Json))!;
        var secondTokens = (await second.Content.ReadFromJsonAsync<AuthTokens>(Json))!;
        Assert.NotEqual(firstTokens.RefreshToken, secondTokens.RefreshToken);
        var again = await client.PostAsJsonAsync("/api/v1/identity/refresh", new { refreshToken = firstTokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [PostgresFact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var client = _factory.CreateClient();
        var tokens = await RegisterAsync(client);

        var logout = await client.PostAsJsonAsync("/api/v1/identity/logout", new { refreshToken = tokens.RefreshToken });
        Assert.True(logout.IsSuccessStatusCode);

        var refresh = await client.PostAsJsonAsync("/api/v1/identity/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    private static async Task<AuthTokens> RegisterAsync(HttpClient client)
    {
        var register = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = $"e2e-{Guid.NewGuid():N}@catrashop.test",
            password = "Sup3r-Secret-Pw!",
            firstName = "Ada",
            lastName = "Lovelace",
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        return (await register.Content.ReadFromJsonAsync<AuthTokens>(Json))!;
    }

    private sealed record AuthTokens(string AccessToken, string RefreshToken);
    private sealed record Profile(Guid Id, string Email, string FirstName, string LastName);
}
