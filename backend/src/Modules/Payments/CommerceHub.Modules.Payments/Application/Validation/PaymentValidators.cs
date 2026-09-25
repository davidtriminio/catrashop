using CommerceHub.Modules.Payments.Application.Contracts;
using FluentValidation;

namespace CommerceHub.Modules.Payments.Application.Validation;

public sealed class InitiatePaymentRequestValidator : AbstractValidator<InitiatePaymentRequest>
{
    public InitiatePaymentRequestValidator() => RuleFor(x => x.OrderId).NotEmpty();
}
