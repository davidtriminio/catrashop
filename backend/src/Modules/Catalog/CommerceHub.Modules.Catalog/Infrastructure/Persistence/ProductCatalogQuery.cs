using CommerceHub.Modules.Catalog.Contracts;
using CommerceHub.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Catalog.Infrastructure.Persistence;

/// <summary>Implements the public <see cref="IProductCatalogQuery"/> over the catalog read model.</summary>
public sealed class ProductCatalogQuery : IProductCatalogQuery
{
    private readonly CatalogDbContext _context;
    public ProductCatalogQuery(CatalogDbContext context) => _context = context;

    public Task<PurchasableVariant?> GetPurchasableVariantAsync(Guid variantId, CancellationToken ct = default) =>
        (from p in _context.Products
         where p.Status == ProductStatus.Active
         from v in p.Variants
         where v.Id == variantId && v.IsActive
         select new PurchasableVariant(p.Id, p.Name, v.Id, v.Sku, v.Price, v.Currency))
        .FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<CatalogSearchResult>> SearchProductsAsync(string term, int max, CancellationToken ct = default)
    {
        var like = $"%{term.Trim()}%";
        var take = Math.Clamp(max, 1, 20);

        return await _context.Products
            .Where(p => p.Status == ProductStatus.Active &&
                        (EF.Functions.ILike(p.Name, like) || EF.Functions.ILike(p.Description, like)))
            .OrderBy(p => p.Name)
            .Take(take)
            .Select(p => new CatalogSearchResult(
                p.Name,
                p.Slug,
                _context.Categories.Where(c => c.Id == p.CategoryId).Select(c => c.Name).FirstOrDefault() ?? string.Empty,
                _context.Brands.Where(b => b.Id == p.BrandId).Select(b => b.Name).FirstOrDefault(),
                p.Variants.Where(v => v.IsActive).Select(v => (decimal?)v.Price).Min(),
                p.Variants.Where(v => v.IsActive).Select(v => v.Currency).FirstOrDefault()))
            .ToListAsync(ct);
    }
}
