using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CommerceHub.IntegrationTests;

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

    private readonly CommerceHubApiFactory _factory;

    public EndToEndJourneyTests(CommerceHubApiFactory factory) => _factory = factory;

    [PostgresFact]
    public async Task Register_then_read_own_profile_with_the_issued_token()
    {
        var client = _factory.CreateClient();
        var email = $"e2e-{Guid.NewGuid():N}@commercehub.test";

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
            email = $"missing-{Guid.NewGuid():N}@commercehub.test",
            password = "whatever-Pw-1!",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record AuthTokens(string AccessToken, string RefreshToken);
    private sealed record Profile(Guid Id, string Email, string FirstName, string LastName);
}
