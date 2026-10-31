using Catrashop.BuildingBlocks.Results;
using Catrashop.BuildingBlocks.Security;
using Catrashop.Modules.Orders.Domain;
using Catrashop.Modules.Payments.Application.Configuration;
using Catrashop.Modules.Payments.Application.Contracts;
using Catrashop.Modules.Payments.Application.Services;
using Catrashop.Modules.Payments.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

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

        group.MapGet("/bank-accounts", (PaymentService payments) => Results.Ok(payments.GetBankAccounts()))
        .RequireAuthorization()
        .WithSummary("The bank accounts a customer can transfer to.");

        group.MapGet("/by-order/{orderId:guid}", async (Guid orderId, ICurrentUser user, PaymentService payments, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();
            return (await payments.GetForOrderAsync(customerId, orderId, ct)).ToHttpResult();
        })
        .RequireAuthorization()
        .WithSummary("Get the payment of one of my orders.");

        group.MapPost("/{id:guid}/transfer-proof", async (
            Guid id, HttpContext http, ICurrentUser user, PaymentService payments, IOptions<PaymentsOptions> options, CancellationToken ct) =>
        {
            if (user.UserId is not { } customerId) return Results.Unauthorized();

            var max = options.Value.MaxReceiptBytes;
            // Cap what the server will read before looking at the form (the receipt plus a little for the text fields).
            http.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = max + 64 * 1024;
            if (!http.Request.HasFormContentType)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["receipt"] = ["Send the receipt as multipart/form-data."] });

            IFormCollection form;
            try { form = await http.Request.ReadFormAsync(ct); }
            catch (Exception ex) when (ex is InvalidDataException or BadHttpRequestException or IOException)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["receipt"] = [PaymentErrors.ReceiptTooLarge.Message] });
            }

            var file = form.Files.GetFile("receipt");
            if (file is null || file.Length == 0)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["receipt"] = [PaymentErrors.ReceiptRequired.Message] });
            if (file.Length > max)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["receipt"] = [PaymentErrors.ReceiptTooLarge.Message] });

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream((int)file.Length);
            await stream.CopyToAsync(buffer, ct);

            return (await payments.SubmitTransferProofAsync(customerId, id, form["bankAccountCode"], form["reference"], buffer.ToArray(), ct)).ToHttpResult();
        })
        .RequireAuthorization()
        .WithSummary("Attach the receipt (JPG, PNG or PDF) of a bank transfer; an operator verifies it.");

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

        group.MapGet("/admin", async (string? status, string? method, PaymentService payments, CancellationToken ct) =>
            Results.Ok(await payments.ListAsync(status, method, ct)))
        .RequirePermission(OrderPermissions.Manage)
        .WithSummary("List payments (e.g. status=UnderReview for the transfers waiting for verification).");

        group.MapGet("/admin/by-order/{orderId:guid}", async (Guid orderId, PaymentService payments, CancellationToken ct) =>
            (await payments.GetForOrderAsAdminAsync(orderId, ct)).ToHttpResult())
        .RequirePermission(OrderPermissions.Manage)
        .WithSummary("The payment of any order.");

        group.MapGet("/admin/{id:guid}/receipt", async (Guid id, PaymentService payments, CancellationToken ct) =>
        {
            var receipt = await payments.GetReceiptAsync(id, ct);
            return receipt.IsSuccess
                ? Results.File(receipt.Value.Content, receipt.Value.ContentType)
                : receipt.ToHttpResult();
        })
        .RequirePermission(OrderPermissions.Manage)
        .WithSummary("Download the receipt a customer attached to a transfer.");

        group.MapPost("/admin/{id:guid}/approve", async (Guid id, PaymentService payments, CancellationToken ct) =>
            (await payments.ApproveTransferAsync(id, ct)).ToHttpResult())
        .RequirePermission(OrderPermissions.Manage)
        .WithSummary("Verify a transfer: captures the payment and marks the order paid.");

        group.MapPost("/admin/{id:guid}/reject", async (
            Guid id, RejectTransferRequest request, IValidator<RejectTransferRequest> validator, PaymentService payments, CancellationToken ct) =>
        {
            if (await Validate(validator, request) is { } problem) return problem;
            return (await payments.RejectTransferAsync(id, request.Reason, ct)).ToHttpResult();
        })
        .RequirePermission(OrderPermissions.Manage)
        .WithSummary("Send a transfer receipt back to the customer with the reason.");

        group.MapPost("/admin/{id:guid}/collect", async (Guid id, PaymentService payments, CancellationToken ct) =>
            (await payments.CollectAsync(id, ct)).ToHttpResult())
        .RequirePermission(OrderPermissions.Manage)
        .WithSummary("Register that the money of a cash-on-delivery / pay-at-store order was collected (after delivery).");

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
