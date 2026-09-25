namespace CommerceHub.Modules.Catalog.Application.Contracts;

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
    string Status);

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

// --- Search -------------------------------------------------------------------------------------

public enum ProductSort
{
    Relevance = 0,
    NameAsc = 1,
    NameDesc = 2,
    Newest = 3
}

/// <summary>Customer-facing product search. Only Active products are ever returned.</summary>
public sealed record ProductSearchCriteria(
    string? SearchTerm,
    string? CategorySlug,
    string? BrandSlug,
    ProductSort Sort,
    int Page,
    int PageSize);
