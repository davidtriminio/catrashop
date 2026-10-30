using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Payments.Application.Abstractions;
using Catrashop.Modules.Payments.Application.Contracts;
using Catrashop.Modules.Payments.Contracts;
using Catrashop.Modules.Payments.Domain;

namespace Catrashop.Modules.Payments.Application.Services;

/// <summary>
/// Drives payments against orders through the configured <see cref="IPaymentProvider"/> and reflects the outcome
/// back onto the order via <see cref="IOrderWorkflow"/>. Charging is idempotent by idempotency key (defaulting to
/// the order id), so a retried request never charges twice.
/// </summary>
public sealed class PaymentService
{
    private readonly IPaymentRepository _payments;
    private readonly IPaymentProvider _provider;
    private readonly IOrderWorkflow _orders;
    private readonly IIntegrationEventPublisher _events;
    private readonly IPaymentUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public PaymentService(
        IPaymentRepository payments,
        IPaymentProvider provider,
        IOrderWorkflow orders,
        IIntegrationEventPublisher events,
        IPaymentUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _payments = payments;
        _provider = provider;
        _orders = orders;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PaymentResponse>> InitiateAsync(Guid customerId, Guid orderId, string? idempotencyKey, CancellationToken ct = default)
    {
        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? $"order:{orderId}" : idempotencyKey.Trim();

        // Idempotent: a prior request with the same key returns the same payment (no re-charge).
        var existing = await _payments.GetByIdempotencyKeyAsync(key, ct);
        if (existing is not null)
            return PaymentMappers.ToResponse(existing);

        var payable = await _orders.GetPayableAsync(orderId, ct);
        if (payable is null || payable.CustomerId != customerId)
            return PaymentErrors.OrderNotFound;
        if (!payable.IsAwaitingPayment)
            return PaymentErrors.OrderNotPayable;

        var now = _clock.GetUtcNow();
        var payment = Payment.Start(orderId, customerId, key, payable.Amount, payable.Currency, _provider.Name, now);
        _payments.Add(payment);

        var result = await _provider.AuthorizeAndCaptureAsync(new PaymentRequest(orderId, key, payable.Amount, payable.Currency), ct);
        if (result.Success)
        {
            payment.MarkCaptured(result.ProviderReference!, now);
            _events.Enqueue(new PaymentCapturedIntegrationEvent(payment.Id, orderId, payment.Amount, payment.Currency), orderId);
        }
        else
        {
            payment.MarkFailed(result.FailureReason, now);
            _events.Enqueue(new PaymentFailedIntegrationEvent(payment.Id, orderId, result.FailureReason), orderId);
        }

        // Persist the money outcome first, then reflect it onto the order (a separate, idempotent unit of work).
        await _unitOfWork.SaveChangesAsync(ct);

        if (payment.IsCaptured)
            await _orders.ConfirmPaymentAsync(orderId, ct);
        else
            await _orders.FailPaymentAsync(orderId, result.FailureReason, ct);

        return PaymentMappers.ToResponse(payment);
    }

    public async Task<Result<PaymentResponse>> GetAsync(Guid customerId, Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null || payment.CustomerId != customerId)
            return PaymentErrors.NotFound;
        return PaymentMappers.ToResponse(payment);
    }

    public async Task<Result<PaymentResponse>> RefundAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return PaymentErrors.NotFound;
        if (!payment.IsCaptured)
            return PaymentErrors.NotRefundable;

        var result = await _provider.RefundAsync(payment.ProviderReference!, payment.Amount, ct);
        if (!result.Success)
            return Error.Conflict("payments.refund_failed", result.FailureReason ?? "The provider declined the refund.");

        var now = _clock.GetUtcNow();
        payment.MarkRefunded(now);
        _events.Enqueue(new RefundCompletedIntegrationEvent(payment.Id, payment.OrderId, payment.Amount, payment.Currency), payment.OrderId);
        await _unitOfWork.SaveChangesAsync(ct);

        await _orders.RefundAsync(payment.OrderId, ct);

        return PaymentMappers.ToResponse(payment);
    }
}
