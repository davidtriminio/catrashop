using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceHub.BuildingBlocks.Caching;

/// <summary>
/// Shared output-cache convention. Endpoints tag themselves with <c>CacheCatalogReads</c>; the API host
/// registers the matching named policy (TTL, vary-by rules, eviction tag). Living in BuildingBlocks lets a
/// module opt an anonymous read into caching without referencing the API host — the same seam
/// <see cref="Security.PermissionPolicy"/> uses for authorization.
/// </summary>
public static class OutputCachePolicies
{
    /// <summary>Named policy for anonymous, non-personalised catalog reads (browse, detail, taxonomy).</summary>
    public const string CatalogReads = "catalog-reads";

    /// <summary>Tag every catalog read carries, so a catalog write can evict the whole set at once.</summary>
    public const string CatalogTag = "catalog";
}

public static class OutputCacheEndpointExtensions
{
    /// <summary>
    /// Opt an anonymous catalog read into the shared <see cref="OutputCachePolicies.CatalogReads"/> policy.
    /// Only apply to responses that are identical for every caller (no auth, no per-user data).
    /// </summary>
    public static TBuilder CacheCatalogReads<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.CacheOutput(OutputCachePolicies.CatalogReads);
}
