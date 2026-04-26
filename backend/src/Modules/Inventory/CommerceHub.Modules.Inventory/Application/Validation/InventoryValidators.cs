using CommerceHub.Modules.Inventory.Application.Contracts;
using FluentValidation;

namespace CommerceHub.Modules.Inventory.Application.Validation;

public sealed class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.InitialOnHand).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(0);
    }
}

public sealed class ReceiveStockRequestValidator : AbstractValidator<ReceiveStockRequest>
{
    public ReceiveStockRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(300);
    }
}

public sealed class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Adjustment delta must be non-zero.");
        RuleFor(x => x.Reason).MaximumLength(300);
    }
}

public sealed class SetThresholdRequestValidator : AbstractValidator<SetThresholdRequest>
{
    public SetThresholdRequestValidator() => RuleFor(x => x.Threshold).GreaterThanOrEqualTo(0);
}
