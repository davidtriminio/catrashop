using CommerceHub.Modules.Catalog.Application.Contracts;
using CommerceHub.BuildingBlocks.Money;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace CommerceHub.Modules.Catalog.Application.Validation;

/// <summary>Variant prices are strictly positive, have at most two decimals and stay below this ceiling.</summary>
public static class PriceLimits
{
    public const decimal Max = 1_000_000m;
}

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreateBrandRequestValidator : AbstractValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator(IOptions<StoreOptions> options)
    {
        var store = options.Value;
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Variants).NotEmpty().WithMessage("A product must have at least one variant.");
        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(i => i.Sku).NotEmpty().MaximumLength(64);
            v.RuleFor(i => i.Name).NotEmpty().MaximumLength(150);
            v.RuleFor(i => i.UnitPrice).GreaterThan(0).PrecisionScale(12, 2, false).LessThanOrEqualTo(PriceLimits.Max);
            v.RuleFor(i => i.Currency).NotEmpty().Must(store.IsStoreCurrency).WithMessage(store.CurrencyMessage);
        });
        RuleForEach(x => x.Images).ChildRules(img =>
            img.RuleFor(i => i.Url).NotEmpty().MaximumLength(2048));
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public sealed class AddVariantRequestValidator : AbstractValidator<AddVariantRequest>
{
    public AddVariantRequestValidator(IOptions<StoreOptions> options)
    {
        var store = options.Value;
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.UnitPrice).GreaterThan(0).PrecisionScale(12, 2, false).LessThanOrEqualTo(PriceLimits.Max);
        RuleFor(x => x.Currency).NotEmpty().Must(store.IsStoreCurrency).WithMessage(store.CurrencyMessage);
    }
}
