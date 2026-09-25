using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Identity.Domain;

/// <summary>Stable, typed failures for the Identity module. Kept in one place so codes don't drift.</summary>
public static class IdentityErrors
{
    public static readonly Error EmailAlreadyInUse =
        Error.Conflict("identity.email_in_use", "An account with this email already exists.");

    // Deliberately generic so we never reveal whether an email exists or the password was wrong.
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("identity.invalid_credentials", "Invalid email or password.");

    public static readonly Error AccountLocked =
        Error.Forbidden("identity.account_locked", "This account is locked.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("identity.invalid_refresh_token", "The refresh token is invalid or expired.");

    public static readonly Error InvalidResetToken =
        Error.Validation("identity.invalid_reset_token", "The password reset token is invalid or expired.");

    public static readonly Error UserNotFound =
        Error.NotFound("identity.user_not_found", "User not found.");
}
