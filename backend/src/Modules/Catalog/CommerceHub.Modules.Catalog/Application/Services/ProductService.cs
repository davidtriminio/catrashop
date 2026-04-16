using CommerceHub.BuildingBlocks.Domain;
using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Catalog.Application.Abstractions;
using CommerceHub.Modules.Catalog.Application.Contracts;
using CommerceHub.Modules.Catalog.Domain;

namespace CommerceHub.Modules.Catalog.Application.Services;

/// <summary>Administrative operations on products, variants, attributes and images.</summary>
public sealed class ProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IBrandRepository _brands;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public ProductService(
        IProductRepository products,
        ICategoryRepository categories,
        IBrandRepository brands,
        ICatalogUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _products = products;
        _categories = categories;
        _brands = brands;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CreatedResource>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        if (await ValidateTaxonomy(request.CategoryId, request.BrandId, ct) is { } taxonomyError)
            return taxonomyError;

        var slug = Slug.From(request.Name);
        if (await _products.SlugExistsAsync(slug, ct))
            return CatalogErrors.SlugInUse(slug);

        var product = Product.Create(request.Name, request.Description, request.CategoryId, request.BrandId, _clock.GetUtcNow());

        foreach (var variant in request.Variants)
        {
            var normalizedSku = variant.Sku.Trim().ToUpperInvariant();
            if (await _products.SkuExistsAsync(normalizedSku, ct))
                return CatalogErrors.SkuInUse(normalizedSku);

            var added = product.AddVariant(variant.Sku, variant.Name);
            if (added.IsFailure)
                return added.Error; // duplicate SKU within the request
        }

        foreach (var attribute in request.Attributes ?? [])
            product.AddAttribute(attribute.Name, attribute.Value);

        foreach (var image in request.Images ?? [])
            product.AddImage(image.Url, image.AltText, image.SortOrder, image.IsPrimary);

        _products.Add(product);
        await _unitOfWork.SaveChangesAsync(ct);

        return new CreatedResource(product.Id, product.Slug);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null)
            return CatalogErrors.ProductNotFound;

        if (await ValidateTaxonomy(request.CategoryId, request.BrandId, ct) is { } taxonomyError)
            return taxonomyError;

        var newSlug = Slug.From(request.Name);
        if (newSlug != product.Slug && await _products.SlugExistsAsync(newSlug, ct))
            return CatalogErrors.SlugInUse(newSlug);

        product.UpdateDetails(request.Name, request.Description, request.CategoryId, request.BrandId);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ProductVariantResponse>> AddVariantAsync(Guid productId, AddVariantRequest request, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(productId, ct);
        if (product is null)
            return CatalogErrors.ProductNotFound;

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        if (await _products.SkuExistsAsync(normalizedSku, ct))
            return CatalogErrors.SkuInUse(normalizedSku);

        var result = product.AddVariant(request.Sku, request.Name);
        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(ct);
        var v = result.Value;
        return new ProductVariantResponse(v.Id, v.Sku, v.Name, v.IsActive);
    }

    public async Task<Result> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null)
            return CatalogErrors.ProductNotFound;

        var result = product.Publish();
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null)
            return CatalogErrors.ProductNotFound;

        product.Archive();
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Error?> ValidateTaxonomy(Guid categoryId, Guid? brandId, CancellationToken ct)
    {
        if (!await _categories.ExistsAsync(categoryId, ct))
            return CatalogErrors.CategoryNotFound;
        if (brandId is { } b && !await _brands.ExistsAsync(b, ct))
            return CatalogErrors.BrandNotFound;
        return null;
    }
}
