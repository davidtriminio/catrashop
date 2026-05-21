namespace CommerceHub.Modules.Catalog.Contracts;

/// <summary>A purchasable variant's identity and current list price, for other modules to capture at add-to-cart.</summary>
public sealed record PurchasableVariant(
    Guid ProductId,
    string ProductName,
    Guid VariantId,
    string Sku,
    decimal UnitPrice,
    string Currency);

/// <summary>
/// Public, in-process query the Cart module uses to look up a variant's price and name when adding an item
/// (so the price is captured server-side, never trusted from the client). Returns null if the variant does not
/// belong to an Active product or is not an active variant — i.e. not purchasable.
/// </summary>
public interface IProductCatalogQuery
{
    Task<PurchasableVariant?> GetPurchasableVariantAsync(Guid variantId, CancellationToken ct = default);
}
