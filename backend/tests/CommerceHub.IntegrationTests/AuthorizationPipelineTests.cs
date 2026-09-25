using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace CommerceHub.IntegrationTests;

/// <summary>
/// Exercises the authentication → authorization → model-binding order on a real permission-protected
/// endpoint (<c>POST /api/v1/catalog/products</c>, which requires <c>catalog.products.manage</c>). None of
/// these paths reaches the database: the pipeline resolves the outcome before the endpoint body runs.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AuthorizationPipelineTests
{
    private const string ProtectedEndpoint = "/api/v1/catalog/categories";
    private const string RequiredPermission = "catalog.products.manage";

    private readonly CommerceHubApiFactory _factory;

    public AuthorizationPipelineTests(CommerceHubApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Protected_endpoint_without_a_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(ProtectedEndpoint, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_with_a_malformed_token_returns_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");

        var response = await client.PostAsync(ProtectedEndpoint, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_user_without_the_permission_is_forbidden_403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.ForUser("some.other.permission"));

        var response = await client.PostAsync(ProtectedEndpoint, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Authorized_user_reaches_validation_with_a_400_for_an_invalid_body()
    {
        // With the required permission the request clears authentication and authorization and reaches the
        // endpoint, whose validator rejects the empty name with a 400 ProblemDetails — before any service
        // or database call. This proves both that the permission grants access and the request-validation
        // stage runs, without needing live infrastructure.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.ForUser(RequiredPermission));

        var response = await client.PostAsync(ProtectedEndpoint, JsonContent("""{"name":"","description":null}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");
}
