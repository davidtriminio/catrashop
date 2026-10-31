using Catrashop.BuildingBlocks.Caching;
using Catrashop.BuildingBlocks.Results;
using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.Catalog.Application.Contracts;
using Catrashop.Modules.Catalog.Application.Services;
using Catrashop.Modules.Catalog.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catrashop.Modules.Catalog.Api;

internal static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog").WithTags("Catalog");

        // --- Public (customer-facing) reads -----------------------------------------------------

        group.MapGet("/products", async (
            string? q, string? category, string? brand, string? sort, decimal? minPrice, decimal? maxPrice, int? page, int? pageSize,
            CatalogQueryService queries, CancellationToken ct) =>
        {
            // Ignore nonsensical bounds instead of failing the request (a stale URL should still show products).
            (minPrice, maxPrice) = PriceFilter.Normalize(minPrice, maxPrice);

            var criteria = new ProductSearchCriteria(
                q, category, brand, ParseSort(sort, hasTerm: !string.IsNullOrWhiteSpace(q)),
                page ?? 1, pageSize ?? 20, minPrice, maxPrice);
            return Results.Ok(await queries.SearchProductsAsync(criteria, ct));
        })
        .CacheCatalogReads()
        .WithSummary("Search active products (full-text, filterable, sorted, paged).");

        group.MapGet("/price-range", async (CatalogQueryService queries, CancellationToken ct) =>
            Results.Ok(await queries.GetPriceRangeAsync(ct)))
        .CacheCatalogReads()
        .WithSummary("Lowest and highest price across active products.");

        group.MapGet("/products/{slug}", async (string slug, CatalogQueryService queries, CancellationToken ct) =>
            (await queries.GetProductBySlugAsync(slug, ct)).ToHttpResult())
        .CacheCatalogReads()
        .WithSummary("Get an active product by slug.");

        group.MapGet("/categories", async (CatalogQueryService queries, CancellationToken ct) =>
            Results.Ok(await queries.ListCategoriesAsync(ct)))
        .CacheCatalogReads()
        .WithSummary("List active categories.");

        group.MapGet("/brands", async (CatalogQueryService queries, CancellationToken ct) =>
            Results.Ok(await queries.ListBrandsAsync(ct)))
        .CacheCatalogReads()
        .WithSummary("List active brands.");

        // --- Admin (require catalog management permission) --------------------------------------

        var admin = group.MapGroup("").RequirePermission(CatalogPermissions.Manage);

        admin.MapPost("/categories", async (
            CreateCategoryRequest request, IValidator<CreateCategoryRequest> validator,
            CategoryService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.CreateAsync(request, ct)).ToHttpResult(
                r => Results.Created($"/api/v1/catalog/categories/{r.Slug}", r));
        })
        .WithSummary("Create a category.");

        admin.MapPost("/brands", async (
            CreateBrandRequest request, IValidator<CreateBrandRequest> validator,
            BrandService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.CreateAsync(request, ct)).ToHttpResult(
                r => Results.Created($"/api/v1/catalog/brands/{r.Slug}", r));
        })
        .WithSummary("Create a brand.");

        admin.MapPost("/products", async (
            CreateProductRequest request, IValidator<CreateProductRequest> validator,
            ProductService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.CreateAsync(request, ct)).ToHttpResult(
                r => Results.Created($"/api/v1/catalog/products/{r.Slug}", r));
        })
        .WithSummary("Create a product with its variants, attributes and images (Draft).");

        admin.MapPut("/products/{id:guid}", async (
            Guid id, UpdateProductRequest request, IValidator<UpdateProductRequest> validator,
            ProductService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.UpdateAsync(id, request, ct)).ToHttpResult();
        })
        .WithSummary("Update a product's core details.");

        admin.MapPost("/products/{id:guid}/variants", async (
            Guid id, AddVariantRequest request, IValidator<AddVariantRequest> validator,
            ProductService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.AddVariantAsync(id, request, ct)).ToHttpResult();
        })
        .WithSummary("Add a variant to a product.");

        admin.MapPost("/products/{id:guid}/publish", async (Guid id, ProductService service, CancellationToken ct) =>
            (await service.PublishAsync(id, ct)).ToHttpResult())
        .WithSummary("Publish a product (requires at least one variant).");

        admin.MapPost("/products/{id:guid}/archive", async (Guid id, ProductService service, CancellationToken ct) =>
            (await service.ArchiveAsync(id, ct)).ToHttpResult())
        .WithSummary("Archive a product.");

        return app;
    }

    private static ProductSort ParseSort(string? sort, bool hasTerm) => sort?.ToLowerInvariant() switch
    {
        "name" => ProductSort.NameAsc,
        "name_desc" => ProductSort.NameDesc,
        "newest" => ProductSort.Newest,
        "price_asc" => ProductSort.PriceAsc,
        "price_desc" => ProductSort.PriceDesc,
        "relevance" => ProductSort.Relevance,
        _ => hasTerm ? ProductSort.Relevance : ProductSort.NameAsc
    };

    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
