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
}
