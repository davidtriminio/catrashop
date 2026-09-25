using CommerceHub.Modules.Customers.Application.Contracts;
using FluentValidation;

namespace CommerceHub.Modules.Customers.Application.Validation;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30);
    }
}

public sealed class PreferencesRequestValidator : AbstractValidator<PreferencesRequest>
{
    public PreferencesRequestValidator() =>
        RuleFor(x => x.PreferredLanguage).NotEmpty().MaximumLength(10);
}

public sealed class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    public AddressRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(2).WithMessage("Country must be a 2-letter ISO code.");
        RuleFor(x => x.Phone).MaximumLength(30);
    }
}

public sealed class AddWishlistItemRequestValidator : AbstractValidator<AddWishlistItemRequest>
{
    public AddWishlistItemRequestValidator() => RuleFor(x => x.ProductId).NotEmpty();
}
