namespace Catrashop.Modules.Identity.Domain;

/// <summary>
/// An append-only record of a security-relevant event. Standalone (not part of the User aggregate)
/// because entries outlive users and must never be mutated. Deliberately stores no secrets.
/// </summary>
public sealed class SecurityAuditLog
{
    private SecurityAuditLog() { } // EF

    public SecurityAuditLog(Guid id, string eventType, Guid? userId, string? detail, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        EventType = eventType;
        UserId = userId;
        Detail = detail;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    /// <summary>e.g. "login_succeeded", "login_failed", "password_reset_requested", "token_refreshed".</summary>
    public string EventType { get; private set; } = default!;

    public Guid? UserId { get; private set; }
    public string? Detail { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}
