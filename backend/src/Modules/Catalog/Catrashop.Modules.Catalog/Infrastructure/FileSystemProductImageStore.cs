using Catrashop.Modules.Catalog.Application.Abstractions;
using Catrashop.Modules.Catalog.Application.Configuration;
using Microsoft.Extensions.Options;

namespace Catrashop.Modules.Catalog.Infrastructure;

/// <summary>
/// Stores product images as files under <see cref="CatalogOptions.ImagesPath"/> (outside the repository). The file
/// name is generated here, so nothing the client sends ever reaches the file system; opening a key re-checks that it
/// resolves inside the folder and that its extension is one we write.
/// </summary>
public sealed class FileSystemProductImageStore : IProductImageStore
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };

    private readonly string _root;

    public FileSystemProductImageStore(IOptions<CatalogOptions> options)
    {
        var configured = options.Value.ImagesPath;
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Catrashop", "product-images")
            : configured);
    }

    public async Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_root);
        var key = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(_root, key), content, ct);
        return key;
    }

    public Task<StoredImage?> OpenAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        if (path is null || !File.Exists(path) || !ContentTypes.TryGetValue(Path.GetExtension(path), out var contentType))
            return Task.FromResult<StoredImage?>(null);

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult<StoredImage?>(new StoredImage(stream, contentType));
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        if (path is not null && File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string? Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Contains('/') || key.Contains('\\'))
            return null;

        var full = Path.GetFullPath(Path.Combine(_root, key));
        return full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? full : null;
    }
}
