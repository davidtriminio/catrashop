namespace Catrashop.Modules.Identity.Domain;

/// <summary>
/// A single-use password-reset token. As with refresh tokens, only the hash is stored. A token is
/// valid until it expires or is consumed. Requesting a new reset supersedes any outstanding one.
/// </summary>
public sealed class PasswordResetToken
{
    private PasswordResetToken() { } // EF

    internal PasswordResetToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc)
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
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public bool IsValid(DateTimeOffset nowUtc) => ConsumedAtUtc is null && nowUtc < ExpiresAtUtc;

    internal void Consume(DateTimeOffset nowUtc) => ConsumedAtUtc = nowUtc;
}
