namespace Catrashop.Modules.Catalog.Application.Contracts;

// --- Admin requests -----------------------------------------------------------------------------

public sealed record CreateCategoryRequest(string Name, string? Description, Guid? ParentId);

public sealed record CreateBrandRequest(string Name, string? Description);

public sealed record ProductVariantInput(string Sku, string Name, decimal UnitPrice, string Currency);
public sealed record ProductAttributeInput(string Name, string Value);
public sealed record ProductImageInput(string Url, string? AltText, int SortOrder, bool IsPrimary);

public sealed record CreateProductRequest(
    string Name,
    string Description,
    Guid CategoryId,
    Guid? BrandId,
    IReadOnlyList<ProductVariantInput> Variants,
    IReadOnlyList<ProductAttributeInput>? Attributes,
    IReadOnlyList<ProductImageInput>? Images);

public sealed record UpdateProductRequest(string Name, string Description, Guid CategoryId, Guid? BrandId);

public sealed record AddVariantRequest(string Sku, string Name, decimal UnitPrice, string Currency);

/// <summary>Changes a variant's name, price and availability (its SKU and currency are fixed).</summary>
public sealed record UpdateVariantRequest(string Name, decimal UnitPrice, bool IsActive);

// --- Responses ----------------------------------------------------------------------------------

public sealed record CreatedResource(Guid Id, string Slug);

public sealed record CategoryResponse(Guid Id, string Name, string Slug, string? Description, Guid? ParentId, bool IsActive);

public sealed record BrandResponse(Guid Id, string Name, string Slug, string? Description, bool IsActive);

public sealed record ProductListItem(
    Guid Id,
    string Name,
    string Slug,
    string CategoryName,
    string? BrandName,
    string? PrimaryImageUrl,
    int VariantCount,
    string Status,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Currency = null);

public sealed record ProductVariantResponse(Guid Id, string Sku, string Name, decimal UnitPrice, string Currency, bool IsActive);
public sealed record ProductAttributeResponse(string Name, string Value);
public sealed record ProductImageResponse(string Url, string? AltText, int SortOrder, bool IsPrimary);

public sealed record ProductDetail(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Status,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    IReadOnlyList<ProductVariantResponse> Variants,
    IReadOnlyList<ProductAttributeResponse> Attributes,
    IReadOnlyList<ProductImageResponse> Images);

/// <summary>An image as the back office sees it: with its id, so it can be removed or made primary.</summary>
public sealed record AdminProductImage(Guid Id, string Url, string? AltText, int SortOrder, bool IsPrimary);

/// <summary>A product in any state (Draft, Active or Archived), for the back office.</summary>
public sealed record AdminProductDetail(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Status,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    IReadOnlyList<ProductVariantResponse> Variants,
    IReadOnlyList<ProductAttributeResponse> Attributes,
    IReadOnlyList<AdminProductImage> Images,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>Back-office product list: any status, optional text filter on name or SKU.</summary>
public sealed record AdminProductSearchCriteria(string? Status, string? SearchTerm, int Page, int PageSize);

// --- Search -------------------------------------------------------------------------------------

public enum ProductSort
{
    Relevance = 0,
    NameAsc = 1,
    NameDesc = 2,
    Newest = 3,
    PriceAsc = 4,
    PriceDesc = 5
}

/// <summary>Customer-facing product search. Only Active products are ever returned.</summary>
public sealed record ProductSearchCriteria(
    string? SearchTerm,
    string? CategorySlug,
    string? BrandSlug,
    ProductSort Sort,
    int Page,
    int PageSize,
    decimal? MinPrice = null,
    decimal? MaxPrice = null);

/// <summary>Lowest and highest active variant price across active products (drives the price filter bounds).</summary>
public sealed record PriceRangeResponse(decimal? Min, decimal? Max, string? Currency);
