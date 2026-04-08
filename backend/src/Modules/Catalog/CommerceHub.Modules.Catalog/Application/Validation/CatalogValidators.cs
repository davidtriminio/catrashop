using CommerceHub.Modules.Catalog.Application.Contracts;
using FluentValidation;

namespace CommerceHub.Modules.Catalog.Application.Validation;

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
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Variants).NotEmpty().WithMessage("A product must have at least one variant.");
        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(i => i.Sku).NotEmpty().MaximumLength(64);
            v.RuleFor(i => i.Name).NotEmpty().MaximumLength(150);
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
    public AddVariantRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
