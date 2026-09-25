using CommerceHub.Modules.Cart.Application.Contracts;
using FluentValidation;

namespace CommerceHub.Modules.Cart.Application.Validation;

public sealed class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(1000);
    }
}

public sealed class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    // Quantity <= 0 is allowed here and interpreted as "remove the line".
    public UpdateCartItemRequestValidator() => RuleFor(x => x.Quantity).LessThanOrEqualTo(1000);
}
