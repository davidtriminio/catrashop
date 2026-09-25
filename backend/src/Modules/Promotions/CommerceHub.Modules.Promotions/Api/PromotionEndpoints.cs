using CommerceHub.BuildingBlocks.Results;
using CommerceHub.BuildingBlocks.Security;
using CommerceHub.Modules.Promotions.Application.Contracts;
using CommerceHub.Modules.Promotions.Application.Services;
using CommerceHub.Modules.Promotions.Contracts;
using CommerceHub.Modules.Promotions.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CommerceHub.Modules.Promotions.Api;

internal static class PromotionEndpoints
{
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/promotions").WithTags("Promotions");

        // --- Customer: preview a coupon against a cart subtotal ----------------------------------
        group.MapPost("/quote", async (
            QuoteCouponRequest request, IValidator<QuoteCouponRequest> validator,
            ICurrentUser user, IPromotionApplication promotions, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            if (await Validate(validator, request) is { } problem) return problem;

            var result = await promotions.QuoteAsync(request.CouponCode, customerId, request.Subtotal, request.Currency, ct);
            return result.ToHttpResult(q => Results.Ok(new CouponQuoteResponse(q.CouponCode, q.DiscountAmount, q.Currency)));
        })
        .RequireAuthorization()
        .WithSummary("Preview the discount a coupon would apply to a subtotal.");

        // --- Admin (promotions.manage) -----------------------------------------------------------
        var admin = app.MapGroup("/api/v1/promotions/admin").WithTags("Promotions (admin)")
            .RequirePermission(PromotionPermissions.Manage);

        admin.MapGet("/coupons", async (CouponAdminService coupons, CancellationToken ct) =>
            Results.Ok(await coupons.ListAsync(ct)))
        .WithSummary("List all coupons.");

        admin.MapGet("/coupons/{id:guid}", async (Guid id, CouponAdminService coupons, CancellationToken ct) =>
            (await coupons.GetAsync(id, ct)).ToHttpResult())
        .WithSummary("Get a coupon.");

        admin.MapPost("/coupons", async (
            CreateCouponRequest request, IValidator<CreateCouponRequest> validator,
            CouponAdminService coupons, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await coupons.CreateAsync(request, ct)).ToHttpResult(c => Results.Created($"/api/v1/promotions/admin/coupons/{c.Id}", c));
        })
        .WithSummary("Create a coupon.");

        admin.MapPut("/coupons/{id:guid}", async (
            Guid id, UpdateCouponRequest request, IValidator<UpdateCouponRequest> validator,
            CouponAdminService coupons, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await coupons.UpdateAsync(id, request, ct)).ToHttpResult();
        })
        .WithSummary("Update a coupon.");

        admin.MapPost("/coupons/{id:guid}/activate", async (Guid id, CouponAdminService coupons, CancellationToken ct) =>
            (await coupons.SetActiveAsync(id, true, ct)).ToHttpResult()).WithSummary("Activate a coupon.");

        admin.MapPost("/coupons/{id:guid}/deactivate", async (Guid id, CouponAdminService coupons, CancellationToken ct) =>
            (await coupons.SetActiveAsync(id, false, ct)).ToHttpResult()).WithSummary("Deactivate a coupon.");

        return app;
    }

    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
