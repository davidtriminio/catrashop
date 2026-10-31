using System.Security.Claims;
using System.Text;
using Catrashop.Modules.Identity.Application.Abstractions;
using Catrashop.Modules.Identity.Application.Configuration;
using Catrashop.Modules.Identity.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Catrashop.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Issues short-lived HS256 JWTs. The token carries the user id (sub), email, one claim per role
/// ("role"), and one per effective permission ("permission") so the API can authorize without a DB
/// round-trip. Authorization decisions read these claims (see the permission policy in the API host).
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    public const string RoleClaimType = "role";
    public const string PermissionClaimType = "permission";

    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;

    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public AccessToken Generate(User user)
    {
        var now = _clock.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(user.Roles.Select(r => new Claim(RoleClaimType, r.ToString())));
        claims.AddRange(user.Permissions.Select(p => new Claim(PermissionClaimType, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessToken(token, expires);
    }
}
