namespace CommerceHub.Modules.Catalog.Domain;

/// <summary>Lifecycle of a product. Only <see cref="Active"/> products are shown to customers.</summary>
public enum ProductStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

/// <summary>
/// A sellable variant of a product, identified by a globally-unique <see cref="Sku"/>. Price lives in
/// the Pricing module and stock in Inventory — Catalog owns only the product structure and identity.
/// The option combination (e.g. "Blue / L") is captured in <see cref="Name"/>.
/// </summary>
public sealed class ProductVariant
{
    private ProductVariant() { } // EF

    internal ProductVariant(Guid id, Guid productId, string sku, string name)
    {
        Id = id;
        ProductId = productId;
        Sku = sku;
        Name = name;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }

    internal void Deactivate() => IsActive = false;
}

/// <summary>A descriptive attribute of a product (e.g. Material = Cotton).</summary>
public sealed class ProductAttribute
{
    private ProductAttribute() { } // EF

    internal ProductAttribute(Guid id, Guid productId, string name, string value)
    {
        Id = id;
        ProductId = productId;
        Name = name;
        Value = value;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Value { get; private set; } = default!;
}

/// <summary>An image for a product. Exactly one image may be primary.</summary>
public sealed class ProductImage
{
    private ProductImage() { } // EF

    internal ProductImage(Guid id, Guid productId, string url, string? altText, int sortOrder, bool isPrimary)
    {
        Id = id;
        ProductId = productId;
        Url = url;
        AltText = altText;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = default!;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
