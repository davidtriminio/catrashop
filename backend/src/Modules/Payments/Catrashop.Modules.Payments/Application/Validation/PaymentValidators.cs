using Catrashop.Modules.Payments.Application.Contracts;
using FluentValidation;

namespace Catrashop.Modules.Payments.Application.Validation;

public sealed class InitiatePaymentRequestValidator : AbstractValidator<InitiatePaymentRequest>
{
    public InitiatePaymentRequestValidator() => RuleFor(x => x.OrderId).NotEmpty();
}
