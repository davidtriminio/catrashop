namespace Catrashop.Modules.Payments.Application.Contracts;

// --- Requests -----------------------------------------------------------------------------------

public sealed record InitiatePaymentRequest(Guid OrderId);

public sealed record RejectTransferRequest(string Reason);

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
    string? FailureReason,
    string? Method = null,
    string? TransferBankAccountCode = null,
    string? TransferReference = null,
    bool HasReceipt = false,
    DateTimeOffset? ProofSubmittedAtUtc = null,
    string? ReviewNote = null,
    DateTimeOffset? CreatedAtUtc = null);

public sealed record BankAccountResponse(string Code, string Bank, string Holder, string AccountNumber, string AccountType);
