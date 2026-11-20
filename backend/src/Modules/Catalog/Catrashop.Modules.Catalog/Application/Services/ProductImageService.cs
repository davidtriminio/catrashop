using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Catalog.Application.Abstractions;
using Catrashop.Modules.Catalog.Application.Configuration;
using Catrashop.Modules.Catalog.Application.Contracts;
using Catrashop.Modules.Catalog.Domain;
using Microsoft.Extensions.Options;

namespace Catrashop.Modules.Catalog.Application.Services;

/// <summary>Upload, remove and reorder the photos of a product.</summary>
public sealed class ProductImageService
{
    /// <summary>Public path under which stored images are served (see the catalog endpoints).</summary>
    public const string UrlPrefix = "/api/v1/catalog/images/";

    private readonly IProductRepository _products;
    private readonly IProductImageStore _store;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly CatalogOptions _options;

    public ProductImageService(
        IProductRepository products, IProductImageStore store, ICatalogUnitOfWork unitOfWork, IOptions<CatalogOptions> options)
    {
        _products = products;
        _store = store;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    public async Task<Result<AdminProductImage>> UploadAsync(Guid productId, byte[] content, string? altText, CancellationToken ct = default)
    {
        if (content.Length == 0)
            return CatalogErrors.ImageRequired;
        if (content.Length > _options.MaxImageBytes)
            return CatalogErrors.ImageTooLarge(_options.MaxImageBytes);
        // The file type comes from the bytes, never from the name or content type the client claims.
        if (ImageFile.Sniff(content) is not { } kind)
            return CatalogErrors.UnsupportedImage;

        var product = await _products.GetByIdAsync(productId, ct);
        if (product is null)
            return CatalogErrors.ProductNotFound;
        if (product.Images.Count >= Product.MaxImages)
            return CatalogErrors.TooManyImages;

        var key = await _store.SaveAsync(content, kind.Extension, ct);
        var attached = product.AttachImage(UrlPrefix + key, string.IsNullOrWhiteSpace(altText) ? null : altText);
        if (attached.IsFailure)
        {
            await _store.DeleteAsync(key, ct);
            return attached.Error;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await _store.DeleteAsync(key, ct); // do not leave an orphan file behind
            throw;
        }

        var i = attached.Value;
        return new AdminProductImage(i.Id, i.Url, i.AltText, i.SortOrder, i.IsPrimary);
    }

    public async Task<Result> RemoveAsync(Guid productId, Guid imageId, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(productId, ct);
        if (product is null)
            return CatalogErrors.ProductNotFound;

        var removed = product.RemoveImage(imageId);
        if (removed.IsFailure)
            return removed.Error;

        await _unitOfWork.SaveChangesAsync(ct);

        // Only files we stored are deleted; an external URL added through the API is just forgotten.
        if (removed.Value.Url.StartsWith(UrlPrefix, StringComparison.Ordinal))
            await _store.DeleteAsync(removed.Value.Url[UrlPrefix.Length..], ct);

        return Result.Success();
    }

    public async Task<Result> SetPrimaryAsync(Guid productId, Guid imageId, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(productId, ct);
        if (product is null)
            return CatalogErrors.ProductNotFound;

        var result = product.SetPrimaryImage(imageId);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public Task<StoredImage?> OpenAsync(string key, CancellationToken ct = default) => _store.OpenAsync(key, ct);
}

/// <summary>Recognises the image formats we accept from their first bytes.</summary>
public static class ImageFile
{
    public sealed record Kind(string Extension, string ContentType);

    public static Kind? Sniff(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return new(".jpg", "image/jpeg");
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return new(".png", "image/png");
        // WebP: "RIFF" <size> "WEBP"
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
            return new(".webp", "image/webp");
        return null;
    }
}
