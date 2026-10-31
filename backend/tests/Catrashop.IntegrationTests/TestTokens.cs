using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Catrashop.IntegrationTests;

/// <summary>
/// Mints HS256 access tokens that match what the Identity module issues (sub / email / role / permission
/// claims, same issuer, audience, and signing key the host validates against). Building tokens here — rather
/// than through the domain — lets authorization tests set the exact permission claims they need without a
/// database or a real user aggregate.
/// </summary>
public static class TestTokens
{
    /// <summary>A token for an authenticated user carrying exactly the given permission claims.</summary>
    public static string ForUser(params string[] permissions) =>
        Build(Guid.NewGuid(), "user@catrashop.test", permissions);

    private static string Build(Guid userId, string email, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CatrashopApiFactory.JwtSigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = CatrashopApiFactory.JwtIssuer,
            Audience = CatrashopApiFactory.JwtAudience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
