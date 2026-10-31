using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

namespace Catrashop.Api.Security;

/// <summary>Requires the current user to hold a specific permission claim.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;
    public string Permission { get; }
}

/// <summary>Grants the requirement when the user carries a matching "permission" claim.</summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var hasPermission = context.User.Claims.Any(c =>
            c.Type == JwtTokenGenerator.PermissionClaimType && c.Value == requirement.Permission);

        if (hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Materializes an authorization policy on demand for any permission-prefixed policy name, so endpoints
/// can require arbitrary permission strings without pre-registering a policy for each. Non-permission
/// policy names fall back to the default provider.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefix = PermissionPolicy.Prefix;

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options) =>
        _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName[Prefix.Length..]))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
