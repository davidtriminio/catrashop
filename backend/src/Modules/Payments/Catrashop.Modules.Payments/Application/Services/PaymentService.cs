using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Payments.Application.Abstractions;
using Catrashop.Modules.Payments.Application.Configuration;
using Catrashop.Modules.Payments.Application.Contracts;
using Catrashop.Modules.Payments.Contracts;
using Catrashop.Modules.Payments.Domain;
using Microsoft.Extensions.Options;

namespace Catrashop.Modules.Payments.Application.Services;

/// <summary>
/// Drives payments against orders through the configured <see cref="IPaymentProvider"/> and reflects the outcome
/// back onto the order via <see cref="IOrderWorkflow"/>. Charging is idempotent by idempotency key (defaulting to
/// the order id), so a retried request never charges twice.
/// <para>
/// What "paying" means depends on the method the customer chose at checkout (<see cref="PaymentMethodCodes"/>):
/// cash on delivery and pay-at-store accept the order now and collect the money later; a bank transfer waits for
/// a receipt that an operator verifies. An order with no method (created before methods existed) is charged through
/// the configured <see cref="IPaymentProvider"/>, which is also where an online method would plug in.
/// </para>
/// </summary>
public sealed class PaymentService
{
    private readonly IPaymentRepository _payments;
    private readonly IPaymentProvider _provider;
    private readonly IOrderWorkflow _orders;
    private readonly IIntegrationEventPublisher _events;
    private readonly IPaymentUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly IReceiptStore _receipts;
    private readonly PaymentsOptions _options;

    public PaymentService(
        IPaymentRepository payments,
        IPaymentProvider provider,
        IOrderWorkflow orders,
        IIntegrationEventPublisher events,
        IPaymentUnitOfWork unitOfWork,
        TimeProvider clock,
        IReceiptStore receipts,
        IOptions<PaymentsOptions> options)
    {
        _payments = payments;
        _provider = provider;
        _orders = orders;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _receipts = receipts;
        _options = options.Value;
    }

    public async Task<Result<PaymentResponse>> InitiateAsync(Guid customerId, Guid orderId, string? idempotencyKey, CancellationToken ct = default)
    {
        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? $"order:{orderId}" : idempotencyKey.Trim();

        // Idempotent: a prior request with the same key returns the same payment (no re-charge).
        var existing = await _payments.GetByIdempotencyKeyAsync(key, ct);
        if (existing is not null)
        {
            // A retry after the payment was saved but the order was not yet accepted finishes the job (idempotent).
            if (PaymentMethodCodes.IsCollectedLater(existing.Method) && existing.Status == PaymentStatus.Pending)
            {
                var accepted = await _orders.ConfirmWithoutPaymentAsync(existing.OrderId, ct);
                if (accepted.IsFailure) return accepted.Error;
            }
            return PaymentMappers.ToResponse(existing);
        }

        var payable = await _orders.GetPayableAsync(orderId, ct);
        if (payable is null || payable.CustomerId != customerId)
            return PaymentErrors.OrderNotFound;
        if (!payable.IsAwaitingPayment)
            return PaymentErrors.OrderNotPayable;

        var now = _clock.GetUtcNow();

        if (payable.PaymentMethod is { } method)
            return await StartManualPaymentAsync(method, orderId, customerId, key, payable, now, ct);

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

        // A manual payment (transfer, cash) is returned by hand outside the system; only a gateway is asked to refund.
        if (payment.Method is null)
        {
            var result = await _provider.RefundAsync(payment.ProviderReference!, payment.Amount, ct);
            if (!result.Success)
                return Error.Conflict("payments.refund_failed", result.FailureReason ?? "The provider declined the refund.");
        }

        var now = _clock.GetUtcNow();
        payment.MarkRefunded(now);
        _events.Enqueue(new RefundCompletedIntegrationEvent(payment.Id, payment.OrderId, payment.Amount, payment.Currency), payment.OrderId);
        await _unitOfWork.SaveChangesAsync(ct);

        await _orders.RefundAsync(payment.OrderId, ct);

        return PaymentMappers.ToResponse(payment);
    }
    // --- Manual methods (PR-051) ----------------------------------------------------------------

    private async Task<Result<PaymentResponse>> StartManualPaymentAsync(
        string method, Guid orderId, Guid customerId, string key, PayableOrder payable, DateTimeOffset now, CancellationToken ct)
    {
        if (!PaymentMethodCodes.All.Contains(method))
            return PaymentErrors.OrderNotPayable;

        var payment = Payment.Start(orderId, customerId, key, payable.Amount, payable.Currency, "manual", now, method);
        _payments.Add(payment);
        await _unitOfWork.SaveChangesAsync(ct);

        // Cash on delivery / pay at the store: the order is accepted now and the money is collected later.
        // A transfer stays awaiting payment until its receipt is submitted and verified.
        if (PaymentMethodCodes.IsCollectedLater(method))
        {
            var accepted = await _orders.ConfirmWithoutPaymentAsync(orderId, ct);
            if (accepted.IsFailure) return accepted.Error;
        }

        return PaymentMappers.ToResponse(payment);
    }

    public IReadOnlyList<BankAccountResponse> GetBankAccounts() =>
        _options.BankAccounts
            .Where(a => !string.IsNullOrWhiteSpace(a.Code))
            .Select(a => new BankAccountResponse(a.Code, a.Bank, a.Holder, a.AccountNumber, a.AccountType))
            .ToArray();

    public async Task<Result<PaymentResponse>> GetForOrderAsync(Guid customerId, Guid orderId, CancellationToken ct = default)
    {
        var payment = await _payments.GetLatestForOrderAsync(orderId, ct);
        if (payment is null || payment.CustomerId != customerId)
            return PaymentErrors.NotFound;
        return PaymentMappers.ToResponse(payment);
    }

    public async Task<Result<PaymentResponse>> GetForOrderAsAdminAsync(Guid orderId, CancellationToken ct = default)
    {
        var payment = await _payments.GetLatestForOrderAsync(orderId, ct);
        return payment is null ? PaymentErrors.NotFound : PaymentMappers.ToResponse(payment);
    }

    /// <summary>The customer attaches the receipt of the bank transfer they made; an operator verifies it next.</summary>
    public async Task<Result<PaymentResponse>> SubmitTransferProofAsync(
        Guid customerId, Guid paymentId, string? bankAccountCode, string? reference, byte[] receipt, CancellationToken ct = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null || payment.CustomerId != customerId)
            return PaymentErrors.NotFound;
        if (payment.Method != PaymentMethodCodes.BankTransfer)
            return PaymentErrors.NotATransfer;
        if (payment.Status != PaymentStatus.Pending)
            return PaymentErrors.NotAwaitingProof;

        var bank = bankAccountCode?.Trim();
        if (string.IsNullOrEmpty(bank) || !GetBankAccounts().Any(a => a.Code == bank))
            return PaymentErrors.UnknownBankAccount;
        var transferReference = reference?.Trim();
        if (string.IsNullOrEmpty(transferReference) || transferReference.Length > 100)
            return PaymentErrors.ReferenceRequired;
        if (receipt.Length == 0)
            return PaymentErrors.ReceiptRequired;
        if (receipt.Length > _options.MaxReceiptBytes)
            return PaymentErrors.ReceiptTooLarge;
        if (ReceiptFile.Sniff(receipt) is not { } kind)
            return PaymentErrors.ReceiptUnsupported;

        var payable = await _orders.GetPayableAsync(payment.OrderId, ct);
        if (payable is null || !payable.IsAwaitingPayment)
            return PaymentErrors.OrderNotPayable;

        var previousKey = payment.ReceiptKey;
        var key = await _receipts.SaveAsync(payment.Id, receipt, kind.Extension, ct);
        var submitted = payment.SubmitTransferProof(bank, transferReference, key, kind.ContentType, _clock.GetUtcNow());
        if (submitted.IsFailure)
        {
            await _receipts.DeleteAsync(key, ct);
            return submitted.Error;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // A receipt replaced after a rejection is no longer needed.
        if (previousKey is not null)
            await _receipts.DeleteAsync(previousKey, ct);

        return PaymentMappers.ToResponse(payment);
    }

    public async Task<IReadOnlyList<PaymentResponse>> ListAsync(string? status, string? method, CancellationToken ct = default)
    {
        PaymentStatus? parsed = Enum.TryParse<PaymentStatus>(status, ignoreCase: true, out var s) ? s : null;
        var payments = await _payments.ListAsync(parsed, method?.Trim().ToUpperInvariant(), 200, ct);
        return payments.Select(PaymentMappers.ToResponse).ToArray();
    }

    public async Task<Result<ReceiptDownload>> GetReceiptAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return PaymentErrors.NotFound;
        if (payment.ReceiptKey is null)
            return PaymentErrors.ReceiptNotFound;

        var stream = await _receipts.OpenAsync(payment.ReceiptKey, ct);
        return stream is null
            ? PaymentErrors.ReceiptNotFound
            : new ReceiptDownload(stream, payment.ReceiptContentType ?? "application/octet-stream");
    }

    /// <summary>An operator verified the transfer: the payment is captured and the order is marked paid.</summary>
    public async Task<Result<PaymentResponse>> ApproveTransferAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return PaymentErrors.NotFound;
        if (payment.Status == PaymentStatus.Captured)
            return PaymentMappers.ToResponse(payment); // idempotent
        if (payment.Status != PaymentStatus.UnderReview)
            return PaymentErrors.NotUnderReview;

        // An order cancelled in the meantime must not be paid.
        var payable = await _orders.GetPayableAsync(payment.OrderId, ct);
        if (payable is null || !payable.IsAwaitingPayment)
            return PaymentErrors.OrderNotPayable;

        var now = _clock.GetUtcNow();
        payment.MarkCaptured($"transfer:{payment.TransferReference}", now);
        _events.Enqueue(new PaymentCapturedIntegrationEvent(payment.Id, payment.OrderId, payment.Amount, payment.Currency), payment.OrderId);
        await _unitOfWork.SaveChangesAsync(ct);

        var confirmed = await _orders.ConfirmPaymentAsync(payment.OrderId, ct);
        if (confirmed.IsFailure) return confirmed.Error;
        return PaymentMappers.ToResponse(payment);
    }

    /// <summary>An operator could not verify the transfer: it goes back to the customer with the reason, who can send another receipt.</summary>
    public async Task<Result<PaymentResponse>> RejectTransferAsync(Guid paymentId, string reason, CancellationToken ct = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return PaymentErrors.NotFound;
        if (payment.Status != PaymentStatus.UnderReview)
            return PaymentErrors.NotUnderReview;

        payment.RejectTransferProof(reason.Trim(), _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(ct);
        return PaymentMappers.ToResponse(payment);
    }

    /// <summary>
    /// An operator registers that the money of a cash-on-delivery or pay-at-store order was collected. Only possible
    /// once the order has been delivered / picked up.
    /// </summary>
    public async Task<Result<PaymentResponse>> CollectAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, ct);
        if (payment is null)
            return PaymentErrors.NotFound;
        if (!PaymentMethodCodes.IsCollectedLater(payment.Method))
            return PaymentErrors.NotCollectedLater;
        if (payment.Status == PaymentStatus.Captured)
            return PaymentMappers.ToResponse(payment); // idempotent

        var payable = await _orders.GetPayableAsync(payment.OrderId, ct);
        if (payable is null || payable.Status != "Delivered")
            return PaymentErrors.OrderNotDelivered;

        var captured = payment.MarkCaptured($"cash:{payment.Id:N}", _clock.GetUtcNow());
        if (captured.IsFailure) return captured.Error;
        _events.Enqueue(new PaymentCapturedIntegrationEvent(payment.Id, payment.OrderId, payment.Amount, payment.Currency), payment.OrderId);
        await _unitOfWork.SaveChangesAsync(ct);

        await _orders.ConfirmPaymentAsync(payment.OrderId, ct);
        return PaymentMappers.ToResponse(payment);
    }
}

public sealed record ReceiptDownload(Stream Content, string ContentType);

/// <summary>Recognizes the receipt formats we accept by their first bytes (the file name and declared type are not trusted).</summary>
internal static class ReceiptFile
{
    public sealed record Kind(string Extension, string ContentType);

    public static Kind? Sniff(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return new(".jpg", "image/jpeg");
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return new(".png", "image/png");
        if (b.Length >= 5 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46 && b[4] == 0x2D) return new(".pdf", "application/pdf");
        return null;
    }
}
