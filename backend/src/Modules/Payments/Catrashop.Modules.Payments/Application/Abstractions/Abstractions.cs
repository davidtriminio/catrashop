using Catrashop.BuildingBlocks.Messaging;
using Catrashop.BuildingBlocks.Persistence;
using Catrashop.Modules.Payments.Domain;

namespace Catrashop.Modules.Payments.Application.Abstractions;

/// <summary>The Payments module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface IPaymentUnitOfWork : IUnitOfWork;

/// <summary>Enqueues integration events into the Payments outbox within the current unit of work.</summary>
public interface IIntegrationEventPublisher
{
    void Enqueue(IIntegrationEvent integrationEvent, Guid? correlationId = null);
}

// --- Payment provider abstraction (Section 7: PaymentProvider / FakePaymentProvider) ------------

public sealed record PaymentRequest(Guid OrderId, string IdempotencyKey, decimal Amount, string Currency);

public sealed record PaymentProviderResult(bool Success, string? ProviderReference, string? FailureReason)
{
    public static PaymentProviderResult Ok(string reference) => new(true, reference, null);
    public static PaymentProviderResult Fail(string reason) => new(false, null, reason);
}

/// <summary>
/// Abstraction over a payment gateway so the domain never couples to a specific provider. Swapping in a real
/// gateway is a new implementation of this interface — nothing in the domain or application changes.
/// </summary>
public interface IPaymentProvider
{
    string Name { get; }
    Task<PaymentProviderResult> AuthorizeAndCaptureAsync(PaymentRequest request, CancellationToken ct = default);
    Task<PaymentProviderResult> RefundAsync(string providerReference, decimal amount, CancellationToken ct = default);
}

/// <summary>Keeps transfer receipts on the server's disk, outside the repository. Keys are generated, never client paths.</summary>
public interface IReceiptStore
{
    Task<string> SaveAsync(Guid paymentId, byte[] content, string extension, CancellationToken ct = default);
    Task<Stream?> OpenAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

/// <summary>Verifies the HMAC signature on an inbound provider webhook (Section 50).</summary>
public interface IWebhookSignatureVerifier
{
    bool Verify(string payload, string? signatureHeader);
}

/// <summary>Write-side gateway for the <see cref="Payment"/> aggregate and webhook dedupe store.</summary>
public interface IPaymentRepository
{
    void Add(Payment payment);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);
    Task<Payment?> GetByProviderReferenceAsync(string providerReference, CancellationToken ct = default);
    Task<Payment?> GetLatestForOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> ListAsync(PaymentStatus? status, string? method, int take, CancellationToken ct = default);

    Task<bool> WebhookEventExistsAsync(string eventId, CancellationToken ct = default);
    void AddWebhookEvent(ProcessedWebhookEvent webhookEvent);
}
