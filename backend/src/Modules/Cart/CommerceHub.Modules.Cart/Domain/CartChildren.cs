namespace CommerceHub.Modules.Cart.Domain;

/// <summary>Lifecycle of a cart. Only an <see cref="Active"/> cart accepts changes.</summary>
public enum CartStatus
{
    Active = 0,
    CheckedOut = 1
}

/// <summary>
/// A line in a cart. The <see cref="UnitPrice"/> and <see cref="ProductName"/> are <b>captured</b> when the
/// item is added (a snapshot), so the cart total is stable even if the catalog price later changes; checkout
/// re-validates against the live price.
/// </summary>
public sealed class CartItem
{
    private CartItem() { } // EF

    internal CartItem(Guid id, Guid cartId, Guid productId, Guid variantId, string sku, string productName,
        decimal unitPrice, string currency, int quantity)
    {
        Id = id;
        CartId = cartId;
        ProductId = productId;
        VariantId = variantId;
        Sku = sku;
        ProductName = productName;
        UnitPrice = unitPrice;
        Currency = currency;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid VariantId { get; private set; }
    public string Sku { get; private set; } = default!;
    public string ProductName { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = default!;
    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal void IncreaseQuantity(int by) => Quantity += by;
    internal void SetQuantity(int quantity) => Quantity = quantity;
}
