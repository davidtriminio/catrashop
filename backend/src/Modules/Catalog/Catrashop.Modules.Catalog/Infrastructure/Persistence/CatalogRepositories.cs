using Catrashop.Modules.Catalog.Application.Abstractions;
using Catrashop.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Catalog.Infrastructure.Persistence;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly CatalogDbContext _context;
    public CategoryRepository(CatalogDbContext context) => _context = context;

    public void Add(Category category) => _context.Categories.Add(category);

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        _context.Categories.AnyAsync(c => c.Id == id, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        _context.Categories.AnyAsync(c => c.Slug == slug, ct);

    public async Task<IReadOnlyList<Category>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
        await _context.Categories
            .Where(c => !activeOnly || c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
}

public sealed class BrandRepository : IBrandRepository
{
    private readonly CatalogDbContext _context;
    public BrandRepository(CatalogDbContext context) => _context = context;

    public void Add(Brand brand) => _context.Brands.Add(brand);

    public Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Brands.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        _context.Brands.AnyAsync(b => b.Id == id, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        _context.Brands.AnyAsync(b => b.Slug == slug, ct);

    public async Task<IReadOnlyList<Brand>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
        await _context.Brands
            .Where(b => !activeOnly || b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);
}

public sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;
    public ProductRepository(CatalogDbContext context) => _context = context;

    public void Add(Product product) => _context.Products.Add(product);

    // Owned collections (variants/attributes/images) are loaded with the product automatically.
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        _context.Products.AnyAsync(p => p.Slug == slug, ct);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default) =>
        _context.Products.AnyAsync(p => p.Variants.Any(v => v.Sku == sku), ct);
}
