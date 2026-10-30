using Catrashop.Modules.Payments.Application.Contracts;
using FluentValidation;

namespace Catrashop.Modules.Payments.Application.Validation;

public sealed class RejectTransferRequestValidator : AbstractValidator<RejectTransferRequest>
{
    public RejectTransferRequestValidator() =>
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("Say why the receipt was not accepted.");
}

public sealed class InitiatePaymentRequestValidator : AbstractValidator<InitiatePaymentRequest>
{
    public InitiatePaymentRequestValidator() => RuleFor(x => x.OrderId).NotEmpty();
}
