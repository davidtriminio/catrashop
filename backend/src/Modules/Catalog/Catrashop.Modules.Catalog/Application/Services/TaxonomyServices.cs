using Catrashop.BuildingBlocks.Domain;
using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Catalog.Application.Abstractions;
using Catrashop.Modules.Catalog.Application.Contracts;
using Catrashop.Modules.Catalog.Domain;

namespace Catrashop.Modules.Catalog.Application.Services;

/// <summary>Administrative operations on categories.</summary>
public sealed class CategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public CategoryService(ICategoryRepository categories, ICatalogUnitOfWork unitOfWork, TimeProvider clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CreatedResource>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (request.ParentId is { } parentId && !await _categories.ExistsAsync(parentId, ct))
            return CatalogErrors.CategoryNotFound;

        var slug = Slug.From(request.Name);
        if (await _categories.SlugExistsAsync(slug, ct))
            return CatalogErrors.SlugInUse(slug);

        var category = Category.Create(request.Name, request.Description, request.ParentId, _clock.GetUtcNow());
        _categories.Add(category);
        await _unitOfWork.SaveChangesAsync(ct);

        return new CreatedResource(category.Id, category.Slug);
    }
}

/// <summary>Administrative operations on brands.</summary>
public sealed class BrandService
{
    private readonly IBrandRepository _brands;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public BrandService(IBrandRepository brands, ICatalogUnitOfWork unitOfWork, TimeProvider clock)
    {
        _brands = brands;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CreatedResource>> CreateAsync(CreateBrandRequest request, CancellationToken ct = default)
    {
        var slug = Slug.From(request.Name);
        if (await _brands.SlugExistsAsync(slug, ct))
            return CatalogErrors.SlugInUse(slug);

        var brand = Brand.Create(request.Name, request.Description, _clock.GetUtcNow());
        _brands.Add(brand);
        await _unitOfWork.SaveChangesAsync(ct);

        return new CreatedResource(brand.Id, brand.Slug);
    }
}
