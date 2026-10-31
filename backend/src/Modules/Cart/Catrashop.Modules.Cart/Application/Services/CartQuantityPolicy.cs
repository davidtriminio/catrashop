using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Cart.Domain;

namespace Catrashop.Modules.Cart.Application.Services;

/// <summary>The quantity rules for a single cart line: at least 1, at most the configured cap, never above stock.</summary>
public static class CartQuantityPolicy
{
    /// <param name="totalQuantity">Quantity the line would hold after the operation.</param>
    /// <param name="maxPerLine">Configured per-line cap.</param>
    /// <param name="available">Units still sellable, or null when stock is not tracked.</param>
    public static Result Check(int totalQuantity, int maxPerLine, int? available)
    {
        if (totalQuantity < 1)
            return CartErrors.QuantityMustBePositive;
        if (totalQuantity > maxPerLine)
            return CartErrors.QuantityExceedsLimit(maxPerLine);
        if (available is { } units && totalQuantity > units)
            return CartErrors.InsufficientStock(units);
        return Result.Success();
    }
}
