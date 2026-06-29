using CommerceHub.Modules.Promotions.Application.Contracts;
using CommerceHub.Modules.Promotions.Domain;
using FluentValidation;

namespace CommerceHub.Modules.Promotions.Application.Validation;

public sealed class CreateCouponRequestValidator : AbstractValidator<CreateCouponRequest>
{
    public CreateCouponRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(500);
        CouponRules.Apply(this);
    }
}

public sealed class UpdateCouponRequestValidator : AbstractValidator<UpdateCouponRequest>
{
    public UpdateCouponRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(500);
        CouponRules.Apply(this);
    }
}

public sealed class QuoteCouponRequestValidator : AbstractValidator<QuoteCouponRequest>
{
    public QuoteCouponRequestValidator()
    {
        RuleFor(x => x.CouponCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Subtotal).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

/// <summary>Shared shape rules for the create/update coupon requests (the domain enforces the deeper invariants).</summary>
internal interface ICouponWriteRequest
{
    DiscountType DiscountType { get; }
    decimal DiscountValue { get; }
    string? Currency { get; }
    decimal MinimumSubtotal { get; }
    decimal? MaxDiscountAmount { get; }
    DateTimeOffset? StartsAtUtc { get; }
    DateTimeOffset? EndsAtUtc { get; }
    int? UsageLimit { get; }
    int? PerCustomerLimit { get; }
}

internal static class CouponRules
{
    public static void Apply<T>(AbstractValidator<T> v) where T : ICouponWriteRequest
    {
        v.RuleFor(x => x.DiscountValue).GreaterThan(0);
        v.RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.Percentage)
            .WithMessage("A percentage discount must be between 0 and 100.");
        v.RuleFor(x => x.Currency).NotEmpty().Length(3)
            .When(x => x.DiscountType == DiscountType.FixedAmount)
            .WithMessage("A fixed-amount coupon requires a 3-letter currency.");
        v.RuleFor(x => x.MinimumSubtotal).GreaterThanOrEqualTo(0);
        v.RuleFor(x => x.MaxDiscountAmount!.Value).GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue);
        v.RuleFor(x => x.UsageLimit!.Value).GreaterThan(0).When(x => x.UsageLimit.HasValue);
        v.RuleFor(x => x.PerCustomerLimit!.Value).GreaterThan(0).When(x => x.PerCustomerLimit.HasValue);
        v.RuleFor(x => x.EndsAtUtc!.Value).GreaterThan(x => x.StartsAtUtc!.Value)
            .When(x => x.StartsAtUtc.HasValue && x.EndsAtUtc.HasValue)
            .WithMessage("The end of the validity window must be after its start.");
    }
}
