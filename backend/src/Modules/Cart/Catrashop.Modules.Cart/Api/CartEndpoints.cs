using Catrashop.BuildingBlocks.Results;
using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.Cart.Application.Contracts;
using Catrashop.Modules.Cart.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catrashop.Modules.Cart.Api;

internal static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        // The cart always belongs to the authenticated caller.
        var group = app.MapGroup("/api/v1/cart").WithTags("Cart").RequireAuthorization();

        group.MapGet("", async (ICurrentUser user, CartService carts, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return Results.Ok(await carts.GetAsync(customerId, ct));
        })
        .WithSummary("Get (or start) the current customer's cart.");

        group.MapPost("/items", async (
            AddCartItemRequest request, IValidator<AddCartItemRequest> validator,
            ICurrentUser user, CartService carts, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            if (await Validate(validator, request) is { } problem) return problem;
            return (await carts.AddItemAsync(customerId, request, ct)).ToHttpResult();
        })
        .WithSummary("Add a variant to the cart (price captured server-side).");

        group.MapPut("/items/{itemId:guid}", async (
            Guid itemId, UpdateCartItemRequest request, IValidator<UpdateCartItemRequest> validator,
            ICurrentUser user, CartService carts, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            if (await Validate(validator, request) is { } problem) return problem;
            return (await carts.UpdateItemAsync(customerId, itemId, request, ct)).ToHttpResult();
        })
        .WithSummary("Change a line's quantity (0 removes it).");

        group.MapDelete("/items/{itemId:guid}", async (
            Guid itemId, ICurrentUser user, CartService carts, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return (await carts.RemoveItemAsync(customerId, itemId, ct)).ToHttpResult();
        })
        .WithSummary("Remove a line from the cart.");

        group.MapDelete("", async (ICurrentUser user, CartService carts, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return (await carts.ClearAsync(customerId, ct)).ToHttpResult();
        })
        .WithSummary("Empty the cart.");

        return app;
    }

    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
