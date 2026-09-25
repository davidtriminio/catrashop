using CommerceHub.BuildingBlocks.Domain;
using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Payments.Domain;

/// <summary>
/// A payment against an order. Carries the amount to charge and the provider outcome. The
/// <see cref="IdempotencyKey"/> (unique) makes a retried "pay this order" request return the same payment
/// instead of charging twice. State moves only through <see cref="PaymentStateMachine"/>-validated steps.
/// </summary>
public sealed class Payment : AggregateRoot<Guid>
{
    private Payment() { } // EF

    private Payment(Guid id, Guid orderId, Guid customerId, string idempotencyKey, decimal amount,
        string currency, string provider, DateTimeOffset nowUtc) : base(id)
    {
        OrderId = orderId;
        CustomerId = customerId;
        IdempotencyKey = idempotencyKey;
        Amount = amount;
        Currency = currency;
        Provider = provider;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string IdempotencyKey { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string Provider { get; private set; } = default!;
    public string? ProviderReference { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Payment Start(Guid orderId, Guid customerId, string idempotencyKey, decimal amount,
        string currency, string provider, DateTimeOffset nowUtc) =>
        new(Guid.NewGuid(), orderId, customerId, idempotencyKey, amount, currency.ToUpperInvariant(), provider, nowUtc);

    public Result MarkAuthorized(string providerReference, DateTimeOffset nowUtc)
    {
        var t = Transition(PaymentStatus.Authorized, nowUtc);
        if (t.IsSuccess) ProviderReference = providerReference;
        return t;
    }

    public Result MarkCaptured(string providerReference, DateTimeOffset nowUtc)
    {
        var t = Transition(PaymentStatus.Captured, nowUtc);
        if (t.IsSuccess) ProviderReference = providerReference;
        return t;
    }

    public Result MarkFailed(string? reason, DateTimeOffset nowUtc)
    {
        var t = Transition(PaymentStatus.Failed, nowUtc);
        if (t.IsSuccess) FailureReason = reason;
        return t;
    }

    public Result MarkRefunded(DateTimeOffset nowUtc) => Transition(PaymentStatus.Refunded, nowUtc);

    public bool IsCaptured => Status == PaymentStatus.Captured;

    private Result Transition(PaymentStatus to, DateTimeOffset nowUtc)
    {
        if (Status == to)
            return Result.Success(); // idempotent no-op
        if (!PaymentStateMachine.CanTransition(Status, to))
            return PaymentErrors.InvalidTransition(Status, to);

        Status = to;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }
}

/// <summary>
/// A record that a provider webhook event was processed, keyed by the provider's event id. Lets webhook
/// handling be idempotent: a redelivered event is recognized and skipped (Section 50).
/// </summary>
public sealed class ProcessedWebhookEvent
{
    private ProcessedWebhookEvent() { } // EF

    public ProcessedWebhookEvent(string eventId, string provider, string eventType, DateTimeOffset receivedAtUtc)
    {
        EventId = eventId;
        Provider = provider;
        EventType = eventType;
        ReceivedAtUtc = receivedAtUtc;
    }

    public string EventId { get; private set; } = default!;
    public string Provider { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public DateTimeOffset ReceivedAtUtc { get; private set; }
}
