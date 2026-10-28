using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Cart.Domain;

public static class CartErrors
{
    public static readonly Error ItemNotFound =
        Error.NotFound("cart.item.not_found", "Cart item not found.");

    public static readonly Error QuantityMustBePositive =
        Error.Validation("cart.quantity_invalid", "Quantity must be greater than zero.");

    public static Error CurrencyMismatch(string cartCurrency, string itemCurrency) =>
        Error.Validation("cart.currency_mismatch",
            $"Cannot add a {itemCurrency} item to a {cartCurrency} cart.");

    public static Error QuantityExceedsLimit(int max) =>
        Error.Validation("cart.quantity_exceeds_limit", $"You can add at most {max} units of the same product.");

    public static Error InsufficientStock(int available) =>
        Error.Conflict("cart.insufficient_stock",
            available == 0 ? "This product is out of stock." : $"Only {available} unit(s) available.");

    public static readonly Error NotActive =
        Error.Conflict("cart.not_active", "The cart is no longer active.");

    public static readonly Error VariantNotPurchasable =
        Error.NotFound("cart.variant_not_purchasable", "That product variant is not available for purchase.");
}
