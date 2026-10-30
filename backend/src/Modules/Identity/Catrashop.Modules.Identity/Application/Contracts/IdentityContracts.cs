namespace Catrashop.Modules.Identity.Application.Contracts;

// --- Requests -----------------------------------------------------------------------------------

public sealed record RegisterCustomerRequest(string Email, string Password, string FirstName, string LastName);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

// --- Responses ----------------------------------------------------------------------------------

/// <summary>Issued on successful login/refresh. The refresh token is opaque and single-use.</summary>
public sealed record AuthTokensResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
