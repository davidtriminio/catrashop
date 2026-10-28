using CommerceHub.Modules.Shipping.Application.Contracts;
using CommerceHub.BuildingBlocks.Money;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace CommerceHub.Modules.Shipping.Application.Validation;

public sealed class CreateShippingMethodRequestValidator : AbstractValidator<CreateShippingMethodRequest>
{
    public CreateShippingMethodRequestValidator(IOptions<StoreOptions> options)
    {
        var store = options.Value;
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Must(store.IsStoreCurrency).WithMessage(store.CurrencyMessage);
        RuleFor(x => x.EstimatedDays).InclusiveBetween(0, 365);
    }
}

public sealed class UpdateShippingMethodRequestValidator : AbstractValidator<UpdateShippingMethodRequest>
{
    public UpdateShippingMethodRequestValidator(IOptions<StoreOptions> options)
    {
        var store = options.Value;
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Must(store.IsStoreCurrency).WithMessage(store.CurrencyMessage);
        RuleFor(x => x.EstimatedDays).InclusiveBetween(0, 365);
    }
}

public sealed class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    public CreateShipmentRequestValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.MethodCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Carrier).MaximumLength(100);
    }
}
