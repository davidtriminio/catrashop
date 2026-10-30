using Catrashop.BuildingBlocks.Pagination;
using Catrashop.Modules.Catalog.Application.Abstractions;
using Catrashop.Modules.Catalog.Application.Contracts;
using Catrashop.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Catalog.Infrastructure.Persistence;

/// <summary>
/// Customer-facing read model. Search uses PostgreSQL full-text search (<c>to_tsvector</c> /
/// <c>websearch_to_tsquery</c>) over product name+description, ranks by relevance, and projects
/// lightweight list items with correlated lookups for category/brand names and the primary image.
/// Only Active products are ever returned.
/// </summary>
public sealed class CatalogReadStore : ICatalogReadStore
{
    private const string FtsConfig = "english";
    private readonly CatalogDbContext _context;

    public CatalogReadStore(CatalogDbContext context) => _context = context;

    public async Task<PagedResult<ProductListItem>> SearchProductsAsync(ProductSearchCriteria criteria, CancellationToken ct = default)
    {
        var page = new PageRequest(criteria.Page, criteria.PageSize);

        var query = _context.Products.Where(p => p.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(criteria.CategorySlug))
        {
            var categorySlug = criteria.CategorySlug.Trim();
            query = query.Where(p => _context.Categories.Any(c => c.Id == p.CategoryId && c.Slug == categorySlug));
        }

        if (!string.IsNullOrWhiteSpace(criteria.BrandSlug))
        {
            var brandSlug = criteria.BrandSlug.Trim();
            query = query.Where(p => p.BrandId != null && _context.Brands.Any(b => b.Id == p.BrandId && b.Slug == brandSlug));
        }

        // Price filter: a product matches when at least one of its active variants is inside the range.
        if (criteria.MinPrice is { } minPrice)
            query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price >= minPrice));
        if (criteria.MaxPrice is { } maxPrice)
            query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price <= maxPrice));

        var term = criteria.SearchTerm?.Trim();
        var hasTerm = !string.IsNullOrWhiteSpace(term);
        if (hasTerm)
        {
            query = query.Where(p =>
                EF.Functions.ToTsVector(FtsConfig, p.Name + " " + p.Description)
                  .Matches(EF.Functions.WebSearchToTsQuery(FtsConfig, term!)));
        }

        var total = await query.LongCountAsync(ct);

        query = (criteria.Sort, hasTerm) switch
        {
            (ProductSort.NameAsc, _) => query.OrderBy(p => p.Name),
            (ProductSort.NameDesc, _) => query.OrderByDescending(p => p.Name),
            (ProductSort.Newest, _) => query.OrderByDescending(p => p.CreatedAtUtc),
            (ProductSort.PriceAsc, _) => query
                .OrderBy(p => p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price))
                .ThenBy(p => p.Name),
            (ProductSort.PriceDesc, _) => query
                .OrderByDescending(p => p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price))
                .ThenBy(p => p.Name),
            (ProductSort.Relevance, true) => query.OrderByDescending(p =>
                EF.Functions.ToTsVector(FtsConfig, p.Name + " " + p.Description)
                  .Rank(EF.Functions.WebSearchToTsQuery(FtsConfig, term!))),
            _ => query.OrderBy(p => p.Name)
        };

        var rows = await query
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Slug,
                p.Status,
                CategoryName = _context.Categories.Where(c => c.Id == p.CategoryId).Select(c => c.Name).FirstOrDefault(),
                BrandName = _context.Brands.Where(b => b.Id == p.BrandId).Select(b => b.Name).FirstOrDefault(),
                PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                VariantCount = p.Variants.Count,
                MinPrice = p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price),
                MaxPrice = p.Variants.Where(v => v.IsActive).Max(v => (decimal?)v.Price),
                Currency = p.Variants.Where(v => v.IsActive).Select(v => v.Currency).FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new ProductListItem(
                r.Id, r.Name, r.Slug, r.CategoryName ?? string.Empty, r.BrandName,
                r.PrimaryImageUrl, r.VariantCount, r.Status.ToString(), r.MinPrice, r.MaxPrice, r.Currency))
            .ToArray();

        return new PagedResult<ProductListItem>(items, page.Page, page.PageSize, total);
    }

    public async Task<PriceRangeResponse> GetPriceRangeAsync(CancellationToken ct = default)
    {
        var prices = _context.Products
            .Where(p => p.Status == ProductStatus.Active)
            .SelectMany(p => p.Variants)
            .Where(v => v.IsActive);

        var min = await prices.MinAsync(v => (decimal?)v.Price, ct);
        var max = await prices.MaxAsync(v => (decimal?)v.Price, ct);
        var currency = await prices.Select(v => v.Currency).FirstOrDefaultAsync(ct);
        return new PriceRangeResponse(min, max, currency);
    }

    public async Task<ProductDetail?> GetProductBySlugAsync(string slug, CancellationToken ct = default)
    {
        // Read-only projection to a DTO: no change tracking needed (owned Variants/Attributes/Images still
        // load with the aggregate). Skipping the change tracker saves the identity-map snapshot on this
        // hot customer-facing detail read.
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == ProductStatus.Active, ct);
        if (product is null)
            return null;

        var categoryName = await _context.Categories
            .Where(c => c.Id == product.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(ct) ?? string.Empty;

        string? brandName = product.BrandId is { } brandId
            ? await _context.Brands.Where(b => b.Id == brandId).Select(b => b.Name).FirstOrDefaultAsync(ct)
            : null;

        return new ProductDetail(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.Status.ToString(),
            product.CategoryId,
            categoryName,
            product.BrandId,
            brandName,
            product.Variants.Where(v => v.IsActive)
                .Select(v => new ProductVariantResponse(v.Id, v.Sku, v.Name, v.Price, v.Currency, v.IsActive)).ToArray(),
            product.Attributes.Select(a => new ProductAttributeResponse(a.Name, a.Value)).ToArray(),
            product.Images.OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageResponse(i.Url, i.AltText, i.SortOrder, i.IsPrimary)).ToArray());
    }
}
