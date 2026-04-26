using CommerceHub.BuildingBlocks.Results;
using CommerceHub.BuildingBlocks.Security;
using CommerceHub.Modules.Inventory.Application.Contracts;
using CommerceHub.Modules.Inventory.Application.Services;
using CommerceHub.Modules.Inventory.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CommerceHub.Modules.Inventory.Api;

internal static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // All inventory management requires the inventory.manage permission.
        var group = app.MapGroup("/api/v1/inventory")
            .WithTags("Inventory")
            .RequirePermission(InventoryPermissions.Manage);

        group.MapPost("/items", async (
            CreateInventoryItemRequest request, IValidator<CreateInventoryItemRequest> validator,
            InventoryAdminService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.CreateItemAsync(request, ct)).ToHttpResult(
                r => Results.Created($"/api/v1/inventory/items/{r.Sku}", r));
        })
        .WithSummary("Start tracking inventory for a SKU.");

        group.MapGet("/items", async (
            bool? lowStockOnly, int? page, int? pageSize, InventoryQueryService queries, CancellationToken ct) =>
                Results.Ok(await queries.SearchAsync(lowStockOnly ?? false, page ?? 1, pageSize ?? 20, ct)))
        .WithSummary("List inventory items (optionally only low-stock).");

        group.MapGet("/items/{sku}", async (string sku, InventoryQueryService queries, CancellationToken ct) =>
            (await queries.GetBySkuAsync(sku, ct)).ToHttpResult())
        .WithSummary("Get the stock position for a SKU.");

        group.MapGet("/items/{sku}/movements", async (
            string sku, int? page, int? pageSize, InventoryQueryService queries, CancellationToken ct) =>
                Results.Ok(await queries.GetMovementsAsync(sku, page ?? 1, pageSize ?? 20, ct)))
        .WithSummary("List the movement history for a SKU.");

        group.MapPost("/items/{sku}/receive", async (
            string sku, ReceiveStockRequest request, IValidator<ReceiveStockRequest> validator,
            InventoryAdminService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.ReceiveAsync(sku, request, ct)).ToHttpResult();
        })
        .WithSummary("Receive stock (increase on-hand).");

        group.MapPost("/items/{sku}/adjust", async (
            string sku, AdjustStockRequest request, IValidator<AdjustStockRequest> validator,
            InventoryAdminService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.AdjustAsync(sku, request, ct)).ToHttpResult();
        })
        .WithSummary("Adjust on-hand stock (manual correction).");

        group.MapPut("/items/{sku}/threshold", async (
            string sku, SetThresholdRequest request, IValidator<SetThresholdRequest> validator,
            InventoryAdminService service, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await service.SetThresholdAsync(sku, request, ct)).ToHttpResult();
        })
        .WithSummary("Set the low-stock threshold for a SKU.");

        return app;
    }

    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
