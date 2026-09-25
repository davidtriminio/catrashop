using CommerceHub.BuildingBlocks.Results;
using CommerceHub.BuildingBlocks.Security;
using CommerceHub.Modules.Identity.Application.Authentication;
using CommerceHub.Modules.Identity.Application.Contracts;
using CommerceHub.Modules.Identity.Application.Passwords;
using CommerceHub.Modules.Identity.Application.Users;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CommerceHub.Modules.Identity.Api;

internal static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/identity").WithTags("Identity");

        group.MapPost("/register", async (
            RegisterCustomerRequest request,
            IValidator<RegisterCustomerRequest> validator,
            AuthenticationService auth,
            CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await auth.RegisterCustomerAsync(request, ct)).ToHttpResult();
        })
        .WithSummary("Register a new customer account and start a session.");

        group.MapPost("/login", async (
            LoginRequest request,
            IValidator<LoginRequest> validator,
            AuthenticationService auth,
            CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await auth.LoginAsync(request, ct)).ToHttpResult();
        })
        .WithSummary("Authenticate with email and password.");

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            IValidator<RefreshTokenRequest> validator,
            AuthenticationService auth,
            CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await auth.RefreshAsync(request, ct)).ToHttpResult();
        })
        .WithSummary("Exchange a refresh token for a new access/refresh pair (rotating).");

        group.MapPost("/logout", async (
            RefreshTokenRequest request,
            AuthenticationService auth,
            CancellationToken ct) =>
                (await auth.LogoutAsync(request, ct)).ToHttpResult())
        .WithSummary("Revoke a refresh token (idempotent).");

        group.MapPost("/password/forgot", async (
            ForgotPasswordRequest request,
            IValidator<ForgotPasswordRequest> validator,
            PasswordRecoveryService passwords,
            CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await passwords.ForgotPasswordAsync(request, ct)).ToHttpResult();
        })
        .WithSummary("Request a password-reset link (always succeeds, never reveals account existence).");

        group.MapPost("/password/reset", async (
            ResetPasswordRequest request,
            IValidator<ResetPasswordRequest> validator,
            PasswordRecoveryService passwords,
            CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await passwords.ResetPasswordAsync(request, ct)).ToHttpResult();
        })
        .WithSummary("Complete a password reset with a valid token.");

        group.MapGet("/me", async (
            ICurrentUser currentUser,
            UserQueryService users,
            CancellationToken ct) =>
        {
            if (currentUser.UserId is not { } userId)
                return Results.Unauthorized();
            return (await users.GetProfileAsync(userId, ct)).ToHttpResult();
        })
        .RequireAuthorization()
        .WithSummary("Return the authenticated user's profile.");

        return app;
    }

    /// <summary>Runs the validator; returns a 400 ValidationProblem result if invalid, else null.</summary>
    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
