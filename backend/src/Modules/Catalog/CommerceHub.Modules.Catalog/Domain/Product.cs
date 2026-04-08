using CommerceHub.BuildingBlocks.Domain;
using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Catalog.Domain;

/// <summary>
/// The product aggregate root: identity, descriptive content, and its variants, attributes and images.
/// Enforces the catalog invariants — unique SKU within the product, a single primary image, and
/// "no publish without a sellable variant". Cross-product SKU/slug uniqueness is enforced at the
/// database level (unique indexes) and checked by the application service for a friendly error.
/// </summary>
public sealed class Product : AggregateRoot<Guid>
{
    private readonly List<ProductVariant> _variants = new();
    private readonly List<ProductAttribute> _attributes = new();
    private readonly List<ProductImage> _images = new();

    private Product() { } // EF

    private Product(Guid id, string name, string slug, string description, Guid categoryId, Guid? brandId, DateTimeOffset nowUtc)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Description = description;
        CategoryId = categoryId;
        BrandId = brandId;
        Status = ProductStatus.Draft;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public Guid CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public ProductStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyCollection<ProductAttribute> Attributes => _attributes.AsReadOnly();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    public static Product Create(string name, string description, Guid categoryId, Guid? brandId, DateTimeOffset nowUtc) =>
        new(Guid.NewGuid(), name.Trim(), BuildingBlocks.Domain.Slug.From(name), description.Trim(), categoryId, brandId, nowUtc);

    public void UpdateDetails(string name, string description, Guid categoryId, Guid? brandId)
    {
        Name = name.Trim();
        Slug = BuildingBlocks.Domain.Slug.From(name);
        Description = description.Trim();
        CategoryId = categoryId;
        BrandId = brandId;
        Touch();
    }

    public Result<ProductVariant> AddVariant(string sku, string name)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        if (_variants.Any(v => v.Sku == normalizedSku))
            return CatalogErrors.DuplicateSkuInProduct;

        var variant = new ProductVariant(Guid.NewGuid(), Id, normalizedSku, name.Trim());
        _variants.Add(variant);
        Touch();
        return variant;
    }

    public void AddAttribute(string name, string value)
    {
        _attributes.Add(new ProductAttribute(Guid.NewGuid(), Id, name.Trim(), value.Trim()));
        Touch();
    }

    public void AddImage(string url, string? altText, int sortOrder, bool isPrimary)
    {
        // Only one image can be primary; a new primary demotes the others.
        if (isPrimary)
            foreach (var existing in _images)
                existing.SetPrimary(false);

        // If this is the first image, it becomes primary by default.
        var makePrimary = isPrimary || _images.Count == 0;
        _images.Add(new ProductImage(Guid.NewGuid(), Id, url.Trim(), altText?.Trim(), sortOrder, makePrimary));
        Touch();
    }

    public Result Publish()
    {
        if (_variants.All(v => !v.IsActive))
            return CatalogErrors.CannotPublishWithoutVariant;

        Status = ProductStatus.Active;
        Touch();
        return Result.Success();
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
