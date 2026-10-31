namespace Catrashop.Modules.Cart.Contracts;

/// <summary>A captured cart line, as seen by the checkout flow.</summary>
public sealed record CartLineSnapshot(
    Guid ProductId,
    Guid VariantId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity);

/// <summary>An immutable snapshot of a customer's active cart for the Orders module to build a checkout from.</summary>
public sealed record CartSnapshot(
    Guid CartId,
    Guid CustomerId,
    string Currency,
    IReadOnlyList<CartLineSnapshot> Lines,
    decimal Subtotal);

/// <summary>
/// Public, in-process contract used by Orders during checkout: read the customer's active cart and, once an
/// order is created, mark the cart checked out so it is no longer mutated.
/// </summary>
public interface ICartQuery
{
    Task<CartSnapshot?> GetActiveCartAsync(Guid customerId, CancellationToken ct = default);
    Task MarkCheckedOutAsync(Guid cartId, CancellationToken ct = default);
}
