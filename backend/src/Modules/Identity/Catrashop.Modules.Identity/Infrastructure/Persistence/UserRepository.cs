using Catrashop.Modules.Identity.Application.Abstractions;
using Catrashop.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>. Owned collections (roles/tokens) are loaded
/// with the user automatically, so token-hash lookups can filter across them in a single query.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context) => _context = context;

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default) =>
        _context.Users.AnyAsync(u => u.Email == normalizedEmail, ct);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash), ct);

    public Task<User?> GetByResetTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.PasswordResetTokens.Any(t => t.TokenHash == tokenHash), ct);

    public void Add(User user) => _context.Users.Add(user);
}
