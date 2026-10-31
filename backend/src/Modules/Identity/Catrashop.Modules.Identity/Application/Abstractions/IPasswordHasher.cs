namespace Catrashop.Modules.Identity.Application.Abstractions;

/// <summary>Hashes and verifies user passwords. Implemented with a slow, salted algorithm (bcrypt).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
