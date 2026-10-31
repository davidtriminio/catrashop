using Catrashop.BuildingBlocks.Results;
using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.Customers.Application.Contracts;
using Catrashop.Modules.Customers.Application.Services;
using Catrashop.Modules.Customers.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catrashop.Modules.Customers.Api;

internal static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        // Everything here acts on the authenticated caller's own profile ("me").
        var group = app.MapGroup("/api/v1/customers/me").WithTags("Customers").RequireAuthorization();

        // --- Profile ----------------------------------------------------------------------------

        group.MapGet("", async (ICurrentUser user, CustomerService customers, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            return (await customers.GetProfileAsync(user.UserId!.Value, ct)).ToHttpResult();
        })
        .WithSummary("Get the current customer's profile (auto-provisioned on first access).");

        group.MapPut("", async (
            UpdateProfileRequest request, IValidator<UpdateProfileRequest> validator,
            ICurrentUser user, CustomerService customers, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            if (await Validate(validator, request) is { } problem) return problem;
            return (await customers.UpdateProfileAsync(user.UserId!.Value, request, ct)).ToHttpResult();
        })
        .WithSummary("Update the current customer's profile.");

        group.MapPut("/preferences", async (
            PreferencesRequest request, IValidator<PreferencesRequest> validator,
            ICurrentUser user, CustomerService customers, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            if (await Validate(validator, request) is { } problem) return problem;
            return (await customers.SetPreferencesAsync(user.UserId!.Value, request, ct)).ToHttpResult();
        })
        .WithSummary("Update notification/locale preferences.");

        // --- Addresses --------------------------------------------------------------------------

        group.MapGet("/addresses", async (ICurrentUser user, CustomerService customers, AddressService addresses, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            return (await addresses.ListAsync(user.UserId!.Value, ct)).ToHttpResult();
        })
        .WithSummary("List the customer's addresses.");

        group.MapPost("/addresses", async (
            AddressRequest request, IValidator<AddressRequest> validator,
            ICurrentUser user, CustomerService customers, AddressService addresses, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            if (await Validate(validator, request) is { } problem) return problem;
            return (await addresses.AddAsync(user.UserId!.Value, request, ct))
                .ToHttpResult(a => Results.Created($"/api/v1/customers/me/addresses/{a.Id}", a));
        })
        .WithSummary("Add an address.");

        group.MapPut("/addresses/{id:guid}", async (
            Guid id, AddressRequest request, IValidator<AddressRequest> validator,
            ICurrentUser user, CustomerService customers, AddressService addresses, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            if (await Validate(validator, request) is { } problem) return problem;
            return (await addresses.UpdateAsync(user.UserId!.Value, id, request, ct)).ToHttpResult();
        })
        .WithSummary("Update an address.");

        group.MapDelete("/addresses/{id:guid}", async (
            Guid id, ICurrentUser user, CustomerService customers, AddressService addresses, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            return (await addresses.RemoveAsync(user.UserId!.Value, id, ct)).ToHttpResult();
        })
        .WithSummary("Remove an address.");

        group.MapPut("/addresses/{id:guid}/default-shipping", async (
            Guid id, ICurrentUser user, CustomerService customers, AddressService addresses, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            return (await addresses.SetDefaultShippingAsync(user.UserId!.Value, id, ct)).ToHttpResult();
        })
        .WithSummary("Set an address as the default shipping address.");

        group.MapPut("/addresses/{id:guid}/default-billing", async (
            Guid id, ICurrentUser user, CustomerService customers, AddressService addresses, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            return (await addresses.SetDefaultBillingAsync(user.UserId!.Value, id, ct)).ToHttpResult();
        })
        .WithSummary("Set an address as the default billing address.");

        // --- Wishlist ---------------------------------------------------------------------------

        group.MapGet("/wishlist", async (ICurrentUser user, CustomerService customers, WishlistService wishlist, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            return (await wishlist.ListAsync(user.UserId!.Value, ct)).ToHttpResult();
        })
        .WithSummary("List wishlist items.");

        group.MapPost("/wishlist", async (
            AddWishlistItemRequest request, IValidator<AddWishlistItemRequest> validator,
            ICurrentUser user, CustomerService customers, WishlistService wishlist, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            if (await Validate(validator, request) is { } problem) return problem;
            return (await wishlist.AddAsync(user.UserId!.Value, request.ProductId, ct)).ToHttpResult();
        })
        .WithSummary("Add a product to the wishlist.");

        group.MapDelete("/wishlist/{productId:guid}", async (
            Guid productId, ICurrentUser user, CustomerService customers, WishlistService wishlist, CancellationToken ct) =>
        {
            if (await Ensure(user, customers, ct) is { } unauthorized) return unauthorized;
            return (await wishlist.RemoveAsync(user.UserId!.Value, productId, ct)).ToHttpResult();
        })
        .WithSummary("Remove a product from the wishlist.");

        return app;
    }

    /// <summary>Guarantees the caller is authenticated and their profile exists; returns 401 if not.</summary>
    private static async Task<IResult?> Ensure(ICurrentUser user, CustomerService customers, CancellationToken ct)
    {
        if (user.UserId is not { } userId)
            return Results.Unauthorized();

        await customers.EnsureCustomerAsync(userId, user.Email ?? string.Empty, string.Empty, string.Empty, ct);
        return null;
    }

    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
