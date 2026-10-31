using Catrashop.Modules.Payments.Application.Contracts;
using Catrashop.Modules.Payments.Domain;

namespace Catrashop.Modules.Payments.Application.Services;

internal static class PaymentMappers
{
    public static PaymentResponse ToResponse(Payment p) =>
        new(p.Id, p.OrderId, p.Status.ToString(), p.Amount, p.Currency, p.ProviderReference, p.FailureReason,
            p.Method, p.TransferBankAccountCode, p.TransferReference, p.ReceiptKey is not null, p.ProofSubmittedAtUtc,
            p.ReviewNote, p.CreatedAtUtc);
}
