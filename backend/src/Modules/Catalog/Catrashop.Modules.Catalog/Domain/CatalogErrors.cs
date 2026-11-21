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

    public static readonly Error VariantNotFound =
        Error.NotFound("catalog.variant.not_found", "Variant not found.");

    public static readonly Error ImageNotFound =
        Error.NotFound("catalog.image.not_found", "Image not found.");

    public static readonly Error TooManyImages =
        Error.Validation("catalog.image.too_many", $"A product can have at most {Product.MaxImages} images.");

    public static readonly Error ImageRequired =
        Error.Validation("catalog.image.required", "Choose an image to upload.");

    public static Error ImageTooLarge(long maxBytes) =>
        Error.Validation("catalog.image.too_large", $"The image is too large (maximum {maxBytes / (1024 * 1024)} MB).");

    public static readonly Error UnsupportedImage =
        Error.Validation("catalog.image.unsupported", "Only JPG, PNG or WebP images are accepted.");

    public static readonly Error CannotDeactivateLastVariant =
        Error.Validation("catalog.variant.last_active", "An active product needs at least one active variant. Archive the product instead.");

    public static readonly Error CannotPublishWithoutVariant =
        Error.Validation("catalog.publish_without_variant", "A product needs at least one variant to be published.");
}
