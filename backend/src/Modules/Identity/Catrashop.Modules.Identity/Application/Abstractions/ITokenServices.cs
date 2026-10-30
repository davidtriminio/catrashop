using Catrashop.Modules.Identity.Domain;

namespace Catrashop.Modules.Identity.Application.Abstractions;

/// <summary>Generates a signed JWT access token carrying the user's identity, roles and permissions.</summary>
public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}

/// <summary>A signed access token and the instant it expires.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

/// <summary>Produces cryptographically-random, URL-safe opaque tokens (refresh / reset secrets).</summary>
public interface ISecureTokenGenerator
{
    string Generate();
}

/// <summary>
/// One-way hash for opaque tokens (refresh / reset). These are high-entropy secrets, so a fast SHA-256
/// is appropriate (unlike passwords, which need a slow hash). We store only the hash.
/// </summary>
public interface ITokenHasher
{
    string Hash(string token);
}
