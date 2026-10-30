using System.Text.Json;
using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Payments.Application.Abstractions;
using Catrashop.Modules.Payments.Application.Contracts;
using Catrashop.Modules.Payments.Contracts;
using Catrashop.Modules.Payments.Domain;

namespace Catrashop.Modules.Payments.Application.Services;

/// <summary>
/// Processes inbound provider webhooks (Section 50): verify the signature, reject forgeries, deduplicate by the
/// provider's event id so redeliveries are safe, then reconcile the payment and order. This is the asynchronous
/// confirmation path that a real gateway would use; it is idempotent and independent of the synchronous capture.
/// </summary>
public sealed class WebhookService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IWebhookSignatureVerifier _verifier;
    private readonly IPaymentRepository _payments;
    private readonly IOrderWorkflow _orders;
    private readonly IIntegrationEventPublisher _events;
    private readonly IPaymentUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public WebhookService(
        IWebhookSignatureVerifier verifier,
        IPaymentRepository payments,
        IOrderWorkflow orders,
        IIntegrationEventPublisher events,
        IPaymentUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _verifier = verifier;
        _payments = payments;
        _orders = orders;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> ProcessAsync(string provider, string rawBody, string? signature, CancellationToken ct = default)
    {
        if (!_verifier.Verify(rawBody, signature))
            return PaymentErrors.InvalidSignature;

        WebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WebhookPayload>(rawBody, Json);
        }
        catch (JsonException)
        {
            payload = null;
        }
        if (payload is null || string.IsNullOrWhiteSpace(payload.Id) || string.IsNullOrWhiteSpace(payload.Type))
            return Error.Validation("payments.webhook_invalid", "The webhook payload could not be parsed.");

        // Idempotent: a redelivered event is acknowledged without re-applying its effect.
        if (await _payments.WebhookEventExistsAsync(payload.Id, ct))
            return Result.Success();

        var now = _clock.GetUtcNow();
        _payments.AddWebhookEvent(new ProcessedWebhookEvent(payload.Id, provider, payload.Type, now));

        var payment = await _payments.GetByProviderReferenceAsync(payload.ProviderReference, ct);
        var action = WebhookAction.None;

        if (payment is not null)
        {
            switch (payload.Type)
            {
                case "payment.captured":
                    if (!payment.IsCaptured && payment.MarkCaptured(payload.ProviderReference, now).IsSuccess)
                        _events.Enqueue(new PaymentCapturedIntegrationEvent(payment.Id, payment.OrderId, payment.Amount, payment.Currency), payment.OrderId);
                    action = WebhookAction.Confirm;
                    break;
                case "payment.failed":
                    payment.MarkFailed("Reported failed by provider webhook", now);
                    action = WebhookAction.Fail;
                    break;
                case "payment.refunded":
                    if (payment.MarkRefunded(now).IsSuccess)
                        _events.Enqueue(new RefundCompletedIntegrationEvent(payment.Id, payment.OrderId, payment.Amount, payment.Currency), payment.OrderId);
                    action = WebhookAction.Refund;
                    break;
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Reflect onto the order (each call is idempotent).
        if (payment is not null)
        {
            switch (action)
            {
                case WebhookAction.Confirm: await _orders.ConfirmPaymentAsync(payment.OrderId, ct); break;
                case WebhookAction.Fail: await _orders.FailPaymentAsync(payment.OrderId, "Payment failed", ct); break;
                case WebhookAction.Refund: await _orders.RefundAsync(payment.OrderId, ct); break;
            }
        }

        return Result.Success();
    }

    private enum WebhookAction { None, Confirm, Fail, Refund }
}
