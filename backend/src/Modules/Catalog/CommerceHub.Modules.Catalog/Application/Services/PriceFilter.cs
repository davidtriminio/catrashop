namespace CommerceHub.Modules.Catalog.Application.Services;

/// <summary>Cleans the public catalog price bounds so a stale or hand-edited URL still shows products.</summary>
public static class PriceFilter
{
    /// <summary>Drops negative bounds and swaps inverted ones instead of failing the request.</summary>
    public static (decimal? Min, decimal? Max) Normalize(decimal? minPrice, decimal? maxPrice)
    {
        if (minPrice is < 0) minPrice = null;
        if (maxPrice is < 0) maxPrice = null;
        if (minPrice is { } lo && maxPrice is { } hi && lo > hi) (minPrice, maxPrice) = (hi, lo);
        return (minPrice, maxPrice);
    }
}
