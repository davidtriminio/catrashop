namespace CommerceHub.Modules.Identity.Application.Configuration;

/// <summary>JWT signing/validation settings. Bound from configuration section "Identity:Jwt".</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Identity:Jwt";

    public string Issuer { get; set; } = "commercehub";
    public string Audience { get; set; } = "commercehub-clients";

    /// <summary>Symmetric signing key. Supplied via configuration/secret — never hard-coded.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
}

/// <summary>Session and reset-token lifetimes. Bound from "Identity:Security".</summary>
public sealed class IdentitySecurityOptions
{
    public const string SectionName = "Identity:Security";

    public int RefreshTokenDays { get; set; } = 14;
    public int PasswordResetTokenHours { get; set; } = 2;
}
