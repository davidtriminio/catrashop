using Microsoft.AspNetCore.Builder;

namespace Catrashop.BuildingBlocks.Security;

/// <summary>
/// Shared convention for permission-based authorization. Endpoints call <c>RequirePermission</c>; the
/// API host's policy provider recognizes the <see cref="Prefix"/> and materializes a matching policy.
/// Living in BuildingBlocks lets every module require permissions without referencing the API host.
/// </summary>
public static class PermissionPolicy
{
    public const string Prefix = "perm:";

    public static string PolicyName(string permission) => Prefix + permission;
}

public static class PermissionEndpointExtensions
{
    /// <summary>Require a specific permission (e.g. "catalog.products.manage") to call this endpoint.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(PermissionPolicy.PolicyName(permission));
}
