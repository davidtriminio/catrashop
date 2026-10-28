namespace CommerceHub.Modules.Identity.Domain;

/// <summary>
/// A refresh token issued to a user session. Only the SHA-256 <see cref="TokenHash"/> is stored —
/// never the raw token — so a database leak does not hand out usable tokens. Tokens are single-use:
/// refreshing rotates to a new token and marks the old one revoked, linking them via
/// <see cref="ReplacedByTokenHash"/> so a whole chain can be revoked if reuse is detected.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken() { } // EF

    internal RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive(DateTimeOffset nowUtc) => RevokedAtUtc is null && nowUtc < ExpiresAtUtc;

    /// <summary>
    /// True when this token was rotated (not logged out or compromised) at most <paramref name="grace"/> ago and is
    /// otherwise still valid. A second client presenting it in that window is a legitimate concurrent refresh
    /// (two tabs, or foreground and background tasks), not a replay.
    /// </summary>
    public bool WasJustRotated(DateTimeOffset nowUtc, TimeSpan grace) =>
        RevokedAtUtc is { } revoked
        && ReplacedByTokenHash is not null
        && nowUtc < ExpiresAtUtc
        && nowUtc - revoked <= grace;

    internal void Revoke(DateTimeOffset nowUtc, string? replacedByTokenHash = null)
    {
        RevokedAtUtc = nowUtc;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
