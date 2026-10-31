using Catrashop.Modules.Shipping.Application.Contracts;
using Catrashop.Modules.Shipping.Domain;
using Catrashop.BuildingBlocks.Money;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Catrashop.Modules.Shipping.Application.Validation;

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

public sealed class CreateShippingZoneRequestValidator : AbstractValidator<CreateShippingZoneRequest>
{
    public CreateShippingZoneRequestValidator(IOptions<StoreOptions> options)
    {
        var store = options.Value;
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.Currency).NotEmpty().Must(store.IsStoreCurrency).WithMessage(store.CurrencyMessage);
        RuleFor(x => x.EstimatedDaysMin).InclusiveBetween(0, 60);
        RuleFor(x => x.EstimatedDaysMax).InclusiveBetween(0, 60).GreaterThanOrEqualTo(x => x.EstimatedDaysMin)
            .WithMessage("The maximum days cannot be less than the minimum.");
        RuleFor(x => x.DepartmentCodes).Must(DepartmentRules.AreValidAndUnique).WithMessage(DepartmentRules.Message);
    }
}

public sealed class UpdateShippingZoneRequestValidator : AbstractValidator<UpdateShippingZoneRequest>
{
    public UpdateShippingZoneRequestValidator(IOptions<StoreOptions> options)
    {
        var store = options.Value;
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.Currency).NotEmpty().Must(store.IsStoreCurrency).WithMessage(store.CurrencyMessage);
        RuleFor(x => x.EstimatedDaysMin).InclusiveBetween(0, 60);
        RuleFor(x => x.EstimatedDaysMax).InclusiveBetween(0, 60).GreaterThanOrEqualTo(x => x.EstimatedDaysMin)
            .WithMessage("The maximum days cannot be less than the minimum.");
        RuleFor(x => x.DepartmentCodes).Must(DepartmentRules.AreValidAndUnique).WithMessage(DepartmentRules.Message);
    }
}

internal static class DepartmentRules
{
    public const string Message = "A zone needs at least one department, each a valid Honduran department code (e.g. HN-FM) listed once.";

    public static bool AreValidAndUnique(IReadOnlyList<string>? codes) =>
        codes is { Count: > 0 }
        && codes.All(HondurasDepartments.IsValid)
        && codes.Select(c => HondurasDepartments.Normalize(c)).Distinct().Count() == codes.Count;
}

public sealed class SavePickupStoreRequestValidator : AbstractValidator<SavePickupStoreRequest>
{
    public SavePickupStoreRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentCode).Must(HondurasDepartments.IsValid)
            .WithMessage("The department must be a valid Honduran department code (e.g. HN-FM).");
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Hours).MaximumLength(200);
    }
}
