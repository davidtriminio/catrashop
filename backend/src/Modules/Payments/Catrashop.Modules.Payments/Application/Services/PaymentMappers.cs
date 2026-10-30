using Catrashop.Modules.Payments.Application.Contracts;
using Catrashop.Modules.Payments.Domain;

namespace Catrashop.Modules.Payments.Application.Services;

internal static class PaymentMappers
{
    public static PaymentResponse ToResponse(Payment p) =>
        new(p.Id, p.OrderId, p.Status.ToString(), p.Amount, p.Currency, p.ProviderReference, p.FailureReason);
}
