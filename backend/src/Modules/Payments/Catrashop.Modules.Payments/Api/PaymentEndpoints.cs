using Catrashop.BuildingBlocks.Results;
using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.Orders.Domain;
using Catrashop.Modules.Payments.Application.Contracts;
using Catrashop.Modules.Payments.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catrashop.Modules.Payments.Api;

internal static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payments").WithTags("Payments");

        group.MapPost("", async (
            InitiatePaymentRequest request, IValidator<InitiatePaymentRequest> validator,
            ICurrentUser user, HttpRequest http, PaymentService payments, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            if (await Validate(validator, request) is { } problem) return problem;

            var idempotencyKey = http.Headers["Idempotency-Key"].FirstOrDefault();
            return (await payments.InitiateAsync(customerId, request.OrderId, idempotencyKey, ct))
                .ToHttpResult(r => Results.Created($"/api/v1/payments/{r.Id}", r));
        })
        .RequireAuthorization()
        .WithSummary("Pay for an order (idempotent via the Idempotency-Key header).");

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUser user, PaymentService payments, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return (await payments.GetAsync(customerId, id, ct)).ToHttpResult();
        })
        .RequireAuthorization()
        .WithSummary("Get one of my payments.");

        group.MapPost("/admin/{id:guid}/refund", async (Guid id, PaymentService payments, CancellationToken ct) =>
            (await payments.RefundAsync(id, ct)).ToHttpResult())
        .RequirePermission(OrderPermissions.Manage)
        .WithSummary("Refund a captured payment (moves the order to Refunded).");

        // Provider webhooks: no user auth — authenticity is the HMAC signature over the raw body (Section 50).
        group.MapPost("/webhooks/{provider}", async (
            string provider, HttpRequest http, WebhookService webhooks, CancellationToken ct) =>
        {
            using var reader = new StreamReader(http.Body);
            var body = await reader.ReadToEndAsync(ct);
            var signature = http.Headers["X-Signature"].FirstOrDefault();
            return (await webhooks.ProcessAsync(provider, body, signature, ct)).ToHttpResult();
        })
        .AllowAnonymous()
        .WithSummary("Receive a signed provider webhook (idempotent).");

        return app;
    }

    private static async Task<IResult?> Validate<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
