using CommerceHub.BuildingBlocks.Pagination;
using CommerceHub.BuildingBlocks.Persistence;
using CommerceHub.Modules.Catalog.Application.Contracts;
using CommerceHub.Modules.Catalog.Domain;

namespace CommerceHub.Modules.Catalog.Application.Abstractions;

/// <summary>The Catalog module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface ICatalogUnitOfWork : IUnitOfWork;

public interface ICategoryRepository
{
    void Add(Category category);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> ListAsync(bool activeOnly, CancellationToken ct = default);
}

public interface IBrandRepository
{
    void Add(Brand brand);
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Brand>> ListAsync(bool activeOnly, CancellationToken ct = default);
}

public interface IProductRepository
{
    void Add(Product product);

    /// <summary>Loads a product with its variants, attributes and images for mutation.</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
}

/// <summary>
/// Read side for the customer-facing catalog. Kept separate from the write repositories because it
/// returns lightweight projections (not aggregates) and uses PostgreSQL full-text search — concerns
/// that belong to infrastructure, behind this interface.
/// </summary>
public interface ICatalogReadStore
{
    Task<PagedResult<ProductListItem>> SearchProductsAsync(ProductSearchCriteria criteria, CancellationToken ct = default);
    Task<ProductDetail?> GetProductBySlugAsync(string slug, CancellationToken ct = default);
}
