using CommerceHub.Modules.Inventory.Application.Contracts;
using FluentValidation;

namespace CommerceHub.Modules.Inventory.Application.Validation;

/// <summary>Sanity ceiling for any single stock figure, so a typo cannot create absurd quantities.</summary>
public static class StockLimits
{
    public const int MaxUnits = 1_000_000;
}

public sealed class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.InitialOnHand).InclusiveBetween(0, StockLimits.MaxUnits);
        RuleFor(x => x.LowStockThreshold).InclusiveBetween(0, StockLimits.MaxUnits);
    }
}

public sealed class ReceiveStockRequestValidator : AbstractValidator<ReceiveStockRequest>
{
    public ReceiveStockRequestValidator()
    {
        RuleFor(x => x.Quantity).InclusiveBetween(1, StockLimits.MaxUnits);
        RuleFor(x => x.Reason).MaximumLength(300);
    }
}

public sealed class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Adjustment delta must be non-zero.");
        RuleFor(x => x.Delta).InclusiveBetween(-StockLimits.MaxUnits, StockLimits.MaxUnits);
        RuleFor(x => x.Reason).MaximumLength(300);
    }
}

public sealed class SetThresholdRequestValidator : AbstractValidator<SetThresholdRequest>
{
    public SetThresholdRequestValidator() => RuleFor(x => x.Threshold).InclusiveBetween(0, StockLimits.MaxUnits);
}
