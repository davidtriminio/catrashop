using CommerceHub.Modules.Orders.Application.Contracts;
using FluentValidation;

namespace CommerceHub.Modules.Orders.Application.Validation;

public sealed class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.DeliveryMode)
            .Must(m => Enum.TryParse<Domain.DeliveryMode>(m, ignoreCase: true, out _))
            .WithMessage("The delivery mode must be 'Delivery' or 'Pickup'.");

        When(IsDelivery, () =>
        {
            RuleFor(x => x.ShippingAddress).NotNull().WithMessage("A delivery needs a shipping address.");
            RuleFor(x => x.ShippingAddress!).SetValidator(new ShippingAddressRequestValidator());
        });

        When(IsPickup, () =>
        {
            RuleFor(x => x.PickupStoreId).NotNull().WithMessage("Choose the store where you will collect the order.");
            RuleFor(x => x.PickupRecipientName).NotEmpty().MaximumLength(150)
                .WithMessage("Say who will collect the order.");
        });
    }

    private static bool IsPickup(CheckoutRequest r) =>
        Enum.TryParse<Domain.DeliveryMode>(r.DeliveryMode, ignoreCase: true, out var mode) && mode == Domain.DeliveryMode.Pickup;

    private static bool IsDelivery(CheckoutRequest r) =>
        Enum.TryParse<Domain.DeliveryMode>(r.DeliveryMode, ignoreCase: true, out var mode) && mode == Domain.DeliveryMode.Delivery;
}

public sealed class ShippingAddressRequestValidator : AbstractValidator<ShippingAddressRequest>
{
    public ShippingAddressRequestValidator()
    {
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        // National shipping: deliveries go to Honduras only, to one of its departments.
        RuleFor(x => x.Country).Must(c => string.Equals(c?.Trim(), "HN", StringComparison.OrdinalIgnoreCase))
            .WithMessage("We only deliver within Honduras (country HN).");
        RuleFor(x => x.DepartmentCode).NotEmpty().WithMessage("Choose the department of the delivery address.");
    }
}

public sealed class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator() => RuleFor(x => x.Reason).MaximumLength(500);
}
