using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Catalog.Domain;

public static class CatalogErrors
{
    public static readonly Error CategoryNotFound =
        Error.NotFound("catalog.category.not_found", "Category not found.");

    public static readonly Error BrandNotFound =
        Error.NotFound("catalog.brand.not_found", "Brand not found.");

    public static readonly Error ProductNotFound =
        Error.NotFound("catalog.product.not_found", "Product not found.");

    public static Error SlugInUse(string slug) =>
        Error.Conflict("catalog.slug_in_use", $"The slug '{slug}' is already in use.");

    public static Error SkuInUse(string sku) =>
        Error.Conflict("catalog.sku_in_use", $"SKU '{sku}' is already in use.");

    public static readonly Error DuplicateSkuInProduct =
        Error.Validation("catalog.duplicate_sku", "A product cannot contain two variants with the same SKU.");

    public static readonly Error CannotPublishWithoutVariant =
        Error.Validation("catalog.publish_without_variant", "A product needs at least one variant to be published.");
}
