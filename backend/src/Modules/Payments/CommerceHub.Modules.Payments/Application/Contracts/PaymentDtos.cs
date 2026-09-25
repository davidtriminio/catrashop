namespace CommerceHub.Modules.Payments.Application.Contracts;

// --- Requests -----------------------------------------------------------------------------------

public sealed record InitiatePaymentRequest(Guid OrderId);

/// <summary>Deserialized webhook body from a provider (Section 50).</summary>
public sealed record WebhookPayload(string Id, string Type, string ProviderReference);

// --- Responses ----------------------------------------------------------------------------------

public sealed record PaymentResponse(
    Guid Id,
    Guid OrderId,
    string Status,
    decimal Amount,
    string Currency,
    string? ProviderReference,
    string? FailureReason);
