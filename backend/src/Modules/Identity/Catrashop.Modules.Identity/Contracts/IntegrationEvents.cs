using Catrashop.BuildingBlocks.Messaging;

namespace Catrashop.Modules.Identity.Contracts;

/// <summary>
/// Published when a new customer account is created. Consumed downstream (e.g. Notifications, and the
/// n8n "welcome email" automation in Section 23). This is a public contract: keep its shape stable.
/// </summary>
public sealed record CustomerRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName) : IntegrationEvent;

/// <summary>
/// Published when a user requests a password reset. Carries the raw, single-use reset token so the
/// Notifications module / n8n can compose the reset link. The token is short-lived and hashed at rest;
/// only this transient event and the resulting email ever see the raw value.
/// </summary>
public sealed record PasswordResetRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string ResetToken,
    DateTimeOffset ExpiresAtUtc) : IntegrationEvent;
