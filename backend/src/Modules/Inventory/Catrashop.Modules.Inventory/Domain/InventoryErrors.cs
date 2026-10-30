using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Inventory.Domain;

public static class InventoryErrors
{
    public static Error ItemNotFound(string sku) =>
        Error.NotFound("inventory.item.not_found", $"No inventory item exists for SKU '{sku}'.");

    public static Error SkuAlreadyTracked(string sku) =>
        Error.Conflict("inventory.item.exists", $"Inventory is already tracked for SKU '{sku}'.");

    public static Error InsufficientStock(string sku, int requested, int available) =>
        Error.Conflict("inventory.insufficient_stock",
            $"Cannot reserve {requested} of '{sku}': only {available} available.");

    public static readonly Error NegativeQuantity =
        Error.Validation("inventory.negative_quantity", "Quantity must be greater than zero.");

    public static readonly Error AdjustmentBelowReserved =
        Error.Conflict("inventory.adjustment_below_reserved",
            "The adjustment would drop on-hand stock below the quantity already reserved.");

    public static readonly Error ReservationNotFound =
        Error.NotFound("inventory.reservation.not_found", "Reservation not found.");
}
