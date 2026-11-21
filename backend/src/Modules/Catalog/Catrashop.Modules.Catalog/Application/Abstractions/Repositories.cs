using Catrashop.BuildingBlocks.Pagination;
using Catrashop.BuildingBlocks.Persistence;
using Catrashop.Modules.Catalog.Application.Contracts;
using Catrashop.Modules.Catalog.Domain;

namespace Catrashop.Modules.Catalog.Application.Abstractions;

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
    Task<PriceRangeResponse> GetPriceRangeAsync(CancellationToken ct = default);

    /// <summary>Back-office list: products in every status (Draft, Active, Archived).</summary>
    Task<PagedResult<ProductListItem>> SearchAdminProductsAsync(AdminProductSearchCriteria criteria, CancellationToken ct = default);

    Task<AdminProductDetail?> GetAdminProductAsync(Guid id, CancellationToken ct = default);
}

/// <summary>An image file opened for reading, with its media type.</summary>
public sealed record StoredImage(Stream Content, string ContentType);

/// <summary>Where uploaded product images live. Implementations generate the key; callers never choose a file name.</summary>
public interface IProductImageStore
{
    /// <summary>Saves the bytes and returns the key (a generated file name).</summary>
    Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default);

    /// <summary>Opens an image, or returns null when the key is unknown or does not resolve inside the store.</summary>
    Task<StoredImage?> OpenAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
