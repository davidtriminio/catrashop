using Catrashop.BuildingBlocks.Results;
using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.Orders.Application.Contracts;
using Catrashop.Modules.Orders.Application.Services;
using Catrashop.Modules.Orders.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catrashop.Modules.Orders.Api;

internal static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        // --- Customer (authenticated, own orders only) ------------------------------------------
        var mine = app.MapGroup("/api/v1/orders").WithTags("Orders").RequireAuthorization();

        mine.MapPost("/checkout", async (
            CheckoutRequest request, IValidator<CheckoutRequest> validator,
            ICurrentUser user, OrderCheckoutService checkout, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            if (await Validate(validator, request) is { } problem) return problem;
            return (await checkout.CheckoutAsync(customerId, request, ct))
                .ToHttpResult(r => Results.Created($"/api/v1/orders/{r.OrderId}", r));
        })
        .WithSummary("Check out the current cart into an order (reserves stock, awaits payment).");

        mine.MapGet("/payment-methods", async (
            string? deliveryMode, string? departmentCode, OrderCheckoutService checkout, CancellationToken ct) =>
            Results.Ok(await checkout.GetPaymentMethodsAsync(deliveryMode ?? "Delivery", departmentCode, ct)))
        .WithSummary("The payment methods allowed for a delivery option (mode + destination department).");

        mine.MapGet("", async (int? page, int? pageSize, ICurrentUser user, OrderQueryService queries, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return Results.Ok(await queries.ListMineAsync(customerId, page ?? 1, pageSize ?? 20, ct));
        })
        .WithSummary("List my orders.");

        mine.MapGet("/{id:guid}", async (Guid id, ICurrentUser user, OrderQueryService queries, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return (await queries.GetMineAsync(customerId, id, ct)).ToHttpResult();
        })
        .WithSummary("Get one of my orders.");

        mine.MapPost("/{id:guid}/cancel", async (
            Guid id, CancelOrderRequest request, ICurrentUser user, OrderManagementService management, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return (await management.CancelAsync(id, customerId, request.Reason, ct)).ToHttpResult();
        })
        .WithSummary("Cancel one of my orders (if its state allows).");

        // --- Admin / fulfillment ----------------------------------------------------------------
        var admin = app.MapGroup("/api/v1/orders/admin").WithTags("Orders (admin)");

        admin.MapGet("", async (string? status, int? page, int? pageSize, OrderQueryService queries, CancellationToken ct) =>
        {
            OrderStatus? parsed = Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var s) ? s : null;
            return Results.Ok(await queries.SearchAsync(parsed, page ?? 1, pageSize ?? 20, ct));
        })
        .RequirePermission(OrderPermissions.ReadAll)
        .WithSummary("List/search all orders (optionally by status).");

        admin.MapGet("/{id:guid}", async (Guid id, OrderQueryService queries, CancellationToken ct) =>
            (await queries.GetAnyAsync(id, ct)).ToHttpResult())
        .RequirePermission(OrderPermissions.ReadAll)
        .WithSummary("Get any order.");

        admin.MapPost("/{id:guid}/process", async (Guid id, OrderManagementService m, CancellationToken ct) =>
            (await m.StartProcessingAsync(id, ct)).ToHttpResult())
        .RequirePermission(OrderPermissions.Manage).WithSummary("Move a paid order into processing.");

        admin.MapPost("/{id:guid}/ship", async (Guid id, OrderManagementService m, CancellationToken ct) =>
            (await m.MarkShippedAsync(id, ct)).ToHttpResult())
        .RequirePermission(OrderPermissions.Manage).WithSummary("Mark an order shipped.");

        admin.MapPost("/{id:guid}/deliver", async (Guid id, OrderManagementService m, CancellationToken ct) =>
            (await m.MarkDeliveredAsync(id, ct)).ToHttpResult())
        .RequirePermission(OrderPermissions.Manage).WithSummary("Mark an order delivered.");

        admin.MapPost("/{id:guid}/cancel", async (Guid id, CancelOrderRequest request, OrderManagementService m, CancellationToken ct) =>
            (await m.CancelAsync(id, null, request.Reason, ct)).ToHttpResult())
        .RequirePermission(OrderPermissions.Manage).WithSummary("Cancel any order.");

        return app;
    }

    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
