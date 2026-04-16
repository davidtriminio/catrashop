using CommerceHub.BuildingBlocks.Pagination;
using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Catalog.Application.Abstractions;
using CommerceHub.Modules.Catalog.Application.Contracts;
using CommerceHub.Modules.Catalog.Domain;

namespace CommerceHub.Modules.Catalog.Application.Services;

/// <summary>Customer-facing catalog reads: product search/detail and taxonomy listings.</summary>
public sealed class CatalogQueryService
{
    private readonly ICatalogReadStore _readStore;
    private readonly ICategoryRepository _categories;
    private readonly IBrandRepository _brands;

    public CatalogQueryService(ICatalogReadStore readStore, ICategoryRepository categories, IBrandRepository brands)
    {
        _readStore = readStore;
        _categories = categories;
        _brands = brands;
    }

    public Task<PagedResult<ProductListItem>> SearchProductsAsync(ProductSearchCriteria criteria, CancellationToken ct = default) =>
        _readStore.SearchProductsAsync(criteria, ct);

    public async Task<Result<ProductDetail>> GetProductBySlugAsync(string slug, CancellationToken ct = default)
    {
        var product = await _readStore.GetProductBySlugAsync(slug, ct);
        return product is null ? CatalogErrors.ProductNotFound : product;
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _categories.ListAsync(activeOnly: true, ct);
        return categories
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Slug, c.Description, c.ParentId, c.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<BrandResponse>> ListBrandsAsync(CancellationToken ct = default)
    {
        var brands = await _brands.ListAsync(activeOnly: true, ct);
        return brands
            .Select(b => new BrandResponse(b.Id, b.Name, b.Slug, b.Description, b.IsActive))
            .ToArray();
    }
}
