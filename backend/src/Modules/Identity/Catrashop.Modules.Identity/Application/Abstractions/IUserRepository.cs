using Catrashop.Modules.Identity.Domain;

namespace Catrashop.Modules.Identity.Application.Abstractions;

/// <summary>
/// Persistence gateway for the <see cref="User"/> aggregate. An explicit repository is justified here
/// (per ADR-001's "repositories only where they add value"): callers need email lookups and token-hash
/// lookups that load the aggregate with its child collections, and the abstraction keeps EF out of the
/// application layer.
/// </summary>
public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads the user owning the refresh token with the given hash (with its tokens).</summary>
    Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Loads the user owning a valid reset token with the given hash.</summary>
    Task<User?> GetByResetTokenHashAsync(string tokenHash, CancellationToken ct = default);

    void Add(User user);
}
