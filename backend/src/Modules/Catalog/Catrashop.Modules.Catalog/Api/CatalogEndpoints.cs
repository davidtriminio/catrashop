using Catrashop.BuildingBlocks.Caching;
using Catrashop.BuildingBlocks.Results;
using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.Catalog.Application.Configuration;
using Catrashop.Modules.Catalog.Application.Contracts;
using Catrashop.Modules.Catalog.Application.Services;
using Catrashop.Modules.Catalog.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
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

        group.MapGet("/images/{key}", async (string key, HttpContext http, ProductImageService images, CancellationToken ct) =>
        {
            var image = await images.OpenAsync(key, ct);
            if (image is null)
                return Results.NotFound();

            // File names are random and never reused, so browsers and CDNs may keep an image for good.
            http.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.File(image.Content, image.ContentType, enableRangeProcessing: false);
        })
        .WithSummary("A product image uploaded from the back office.");

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

        admin.MapGet("/admin/products", async (
            string? status, string? q, int? page, int? pageSize, CatalogQueryService queries, CancellationToken ct) =>
            Results.Ok(await queries.SearchAdminProductsAsync(new AdminProductSearchCriteria(status, q, page ?? 1, pageSize ?? 20), ct)))
        .WithSummary("List products in any status (Draft, Active, Archived), filterable by status and by name or SKU.");

        admin.MapGet("/admin/products/{id:guid}", async (Guid id, CatalogQueryService queries, CancellationToken ct) =>
            (await queries.GetAdminProductAsync(id, ct)).ToHttpResult())
        .WithSummary("A product in any status, with variants, attributes and images (with their ids).");

        admin.MapPut("/products/{id:guid}/variants/{variantId:guid}", async (
            Guid id, Guid variantId, UpdateVariantRequest request, IValidator<UpdateVariantRequest> validator,
            ProductService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.UpdateVariantAsync(id, variantId, request, ct)).ToHttpResult();
        })
        .WithSummary("Change a variant's name, price and availability.");

        admin.MapPost("/products/{id:guid}/images", async (
            Guid id, HttpContext http, ProductImageService images, IOptions<CatalogOptions> options, CancellationToken ct) =>
        {
            var max = options.Value.MaxImageBytes;
            // Cap what the server will read before looking at the form (the image plus a little for the text fields).
            http.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = max + 64 * 1024;
            if (!http.Request.HasFormContentType)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = ["Send the image as multipart/form-data."] });

            IFormCollection form;
            try { form = await http.Request.ReadFormAsync(ct); }
            catch (Exception ex) when (ex is InvalidDataException or BadHttpRequestException or IOException)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = [CatalogErrors.ImageTooLarge(max).Message] });
            }

            var file = form.Files.GetFile("image");
            if (file is null || file.Length == 0)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = [CatalogErrors.ImageRequired.Message] });
            if (file.Length > max)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = [CatalogErrors.ImageTooLarge(max).Message] });

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream((int)file.Length);
            await stream.CopyToAsync(buffer, ct);

            return (await images.UploadAsync(id, buffer.ToArray(), form["altText"], ct))
                .ToHttpResult(r => Results.Created(r.Url, r));
        })
        .WithSummary("Upload a product photo (JPG, PNG or WebP).");

        admin.MapPost("/products/{id:guid}/images/{imageId:guid}/primary", async (
            Guid id, Guid imageId, ProductImageService images, CancellationToken ct) =>
            (await images.SetPrimaryAsync(id, imageId, ct)).ToHttpResult())
        .WithSummary("Make an image the product's main photo.");

        admin.MapDelete("/products/{id:guid}/images/{imageId:guid}", async (
            Guid id, Guid imageId, ProductImageService images, CancellationToken ct) =>
            (await images.RemoveAsync(id, imageId, ct)).ToHttpResult())
        .WithSummary("Remove a product photo.");

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
