using CommerceHub.BuildingBlocks.Results;
using CommerceHub.BuildingBlocks.Security;
using CommerceHub.Modules.Orders.Domain;
using CommerceHub.Modules.Shipping.Application.Contracts;
using CommerceHub.Modules.Shipping.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CommerceHub.Modules.Shipping.Api;

internal static class ShippingEndpoints
{
    public static IEndpointRouteBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/shipping").WithTags("Shipping");

        // --- Public ------------------------------------------------------------------------------
        group.MapGet("/methods", async (ShippingMethodService methods, CancellationToken ct) =>
            Results.Ok(await methods.ListAsync(activeOnly: true, ct)))
        .WithSummary("List available shipping methods.");

        group.MapGet("/track/{trackingNumber}", async (string trackingNumber, ShipmentService shipments, CancellationToken ct) =>
            (await shipments.TrackAsync(trackingNumber, ct)).ToHttpResult())
        .WithSummary("Track a shipment by its tracking number (public, limited detail).");

        // --- Customer ----------------------------------------------------------------------------
        group.MapGet("/orders/{orderId:guid}/shipment", async (
            Guid orderId, ICurrentUser user, ShipmentService shipments, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return (await shipments.GetForCustomerAsync(customerId, orderId, ct)).ToHttpResult();
        })
        .RequireAuthorization()
        .WithSummary("Get the shipment for one of my orders.");

        // --- Admin / operations (orders.manage) --------------------------------------------------
        var admin = app.MapGroup("/api/v1/shipping/admin").WithTags("Shipping (admin)")
            .RequirePermission(OrderPermissions.Manage);

        admin.MapPost("/methods", async (
            CreateShippingMethodRequest request, IValidator<CreateShippingMethodRequest> validator,
            ShippingMethodService methods, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await methods.CreateAsync(request, ct)).ToHttpResult(m => Results.Created($"/api/v1/shipping/admin/methods/{m.Id}", m));
        })
        .WithSummary("Create a shipping method.");

        admin.MapPut("/methods/{id:guid}", async (
            Guid id, UpdateShippingMethodRequest request, IValidator<UpdateShippingMethodRequest> validator,
            ShippingMethodService methods, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await methods.UpdateAsync(id, request, ct)).ToHttpResult();
        })
        .WithSummary("Update a shipping method.");

        admin.MapPost("/methods/{id:guid}/activate", async (Guid id, ShippingMethodService methods, CancellationToken ct) =>
            (await methods.SetActiveAsync(id, true, ct)).ToHttpResult()).WithSummary("Activate a shipping method.");

        admin.MapPost("/methods/{id:guid}/deactivate", async (Guid id, ShippingMethodService methods, CancellationToken ct) =>
            (await methods.SetActiveAsync(id, false, ct)).ToHttpResult()).WithSummary("Deactivate a shipping method.");

        admin.MapPost("/shipments", async (
            CreateShipmentRequest request, IValidator<CreateShipmentRequest> validator,
            ShipmentService shipments, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await shipments.CreateAsync(request, ct)).ToHttpResult(s => Results.Created($"/api/v1/shipping/admin/shipments/{s.Id}", s));
        })
        .WithSummary("Create a shipment for a paid order (moves it to Processing).");

        admin.MapGet("/shipments/{id:guid}", async (Guid id, ShipmentService shipments, CancellationToken ct) =>
            (await shipments.GetAsync(id, ct)).ToHttpResult()).WithSummary("Get a shipment.");

        admin.MapPost("/shipments/{id:guid}/ship", async (Guid id, ShipmentService shipments, CancellationToken ct) =>
            (await shipments.MarkShippedAsync(id, ct)).ToHttpResult()).WithSummary("Mark a shipment shipped (order → Shipped).");

        admin.MapPost("/shipments/{id:guid}/deliver", async (Guid id, ShipmentService shipments, CancellationToken ct) =>
            (await shipments.MarkDeliveredAsync(id, ct)).ToHttpResult()).WithSummary("Mark a shipment delivered (order → Delivered).");

        admin.MapPost("/shipments/{id:guid}/cancel", async (Guid id, ShipmentService shipments, CancellationToken ct) =>
            (await shipments.CancelAsync(id, null, ct)).ToHttpResult()).WithSummary("Cancel a pending shipment.");

        return app;
    }

    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
