using Catrashop.BuildingBlocks.Domain;

namespace Catrashop.Modules.Identity.Domain;

/// <summary>
/// The user account aggregate root. It owns its role assignments, refresh tokens, and any outstanding
/// password-reset token, and guards the invariants around them (unique roles, single-use tokens,
/// revoke-all-on-password-change). All token values are supplied pre-hashed by the application layer —
/// the domain never sees raw secrets.
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    private readonly List<UserRole> _roleAssignments = new();
    private readonly List<RefreshToken> _refreshTokens = new();
    private readonly List<PasswordResetToken> _passwordResetTokens = new();

    private User() { } // EF

    private User(Guid id, string email, string passwordHash, string firstName, string lastName, DateTimeOffset nowUtc)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Status = UserStatus.Active;
        EmailConfirmed = false;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public UserStatus Status { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<UserRole> RoleAssignments => _roleAssignments.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<PasswordResetToken> PasswordResetTokens => _passwordResetTokens.AsReadOnly();

    /// <summary>Distinct roles currently assigned to the user.</summary>
    public IReadOnlySet<Role> Roles => _roleAssignments.Select(r => r.Role).ToHashSet();

    /// <summary>Effective permissions, derived from the assigned roles.</summary>
    public IReadOnlySet<string> Permissions => RolePermissions.For(Roles);

    // --- Factory --------------------------------------------------------------------------------

    public static User Register(
        string normalizedEmail,
        string passwordHash,
        string firstName,
        string lastName,
        Role initialRole,
        DateTimeOffset nowUtc)
    {
        var user = new User(Guid.NewGuid(), normalizedEmail, passwordHash, firstName, lastName, nowUtc);
        user.AddRole(initialRole);
        return user;
    }

    // --- Roles ----------------------------------------------------------------------------------

    public void AddRole(Role role)
    {
        if (_roleAssignments.Any(r => r.Role == role))
            return;
        _roleAssignments.Add(new UserRole(Id, role));
        Touch();
    }

    public void RemoveRole(Role role)
    {
        var existing = _roleAssignments.FirstOrDefault(r => r.Role == role);
        if (existing is null)
            return;
        _roleAssignments.Remove(existing);
        Touch();
    }

    // --- Account state --------------------------------------------------------------------------

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        Touch();
    }

    public void Lock() { Status = UserStatus.Locked; Touch(); }
    public void Unlock() { Status = UserStatus.Active; Touch(); }

    public bool CanAuthenticate => Status == UserStatus.Active;

    // --- Refresh tokens (session management) ----------------------------------------------------

    public RefreshToken IssueRefreshToken(string tokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset nowUtc)
    {
        var token = new RefreshToken(Guid.NewGuid(), Id, tokenHash, expiresAtUtc, nowUtc);
        _refreshTokens.Add(token);
        return token;
    }

    public RefreshToken? FindRefreshToken(string tokenHash) =>
        _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

    /// <summary>Rotate a token: revoke the current one (linked to its replacement) and issue a new one.</summary>
    public RefreshToken RotateRefreshToken(RefreshToken current, string newTokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset nowUtc)
    {
        var replacement = new RefreshToken(Guid.NewGuid(), Id, newTokenHash, expiresAtUtc, nowUtc);
        current.Revoke(nowUtc, newTokenHash);
        _refreshTokens.Add(replacement);
        return replacement;
    }

    public void RevokeRefreshToken(RefreshToken token, DateTimeOffset nowUtc) => token.Revoke(nowUtc);

    public void RevokeAllRefreshTokens(DateTimeOffset nowUtc)
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive(nowUtc)))
            token.Revoke(nowUtc);
    }

    // --- Password reset -------------------------------------------------------------------------

    public PasswordResetToken IssuePasswordReset(string tokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset nowUtc)
    {
        // A new request supersedes any outstanding, still-valid reset token.
        foreach (var outstanding in _passwordResetTokens.Where(t => t.IsValid(nowUtc)))
            outstanding.Consume(nowUtc);

        var token = new PasswordResetToken(Guid.NewGuid(), Id, tokenHash, expiresAtUtc, nowUtc);
        _passwordResetTokens.Add(token);
        return token;
    }

    public PasswordResetToken? FindValidPasswordReset(string tokenHash, DateTimeOffset nowUtc) =>
        _passwordResetTokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.IsValid(nowUtc));

    /// <summary>
    /// Complete a password reset: consume the token, set the new hash, and revoke every active session
    /// (an attacker who triggered the reset must not keep a live token).
    /// </summary>
    public void ResetPassword(PasswordResetToken token, string newPasswordHash, DateTimeOffset nowUtc)
    {
        token.Consume(nowUtc);
        PasswordHash = newPasswordHash;
        RevokeAllRefreshTokens(nowUtc);
        Touch();
    }

    public void ChangePasswordHash(string newPasswordHash, DateTimeOffset nowUtc)
    {
        PasswordHash = newPasswordHash;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
