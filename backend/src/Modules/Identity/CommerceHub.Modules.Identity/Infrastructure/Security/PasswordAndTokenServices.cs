using System.Security.Cryptography;
using System.Text;
using CommerceHub.Modules.Identity.Application.Abstractions;

namespace CommerceHub.Modules.Identity.Infrastructure.Security;

/// <summary>bcrypt password hashing. The work factor is deliberately slow to resist offline cracking.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Malformed stored hash — treat as a failed verification rather than throwing.
            return false;
        }
    }
}

/// <summary>256-bit cryptographically-random tokens, hex-encoded (URL-safe).</summary>
public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    public string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}

/// <summary>SHA-256 hashing for opaque high-entropy tokens (refresh / reset) stored at rest.</summary>
public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
