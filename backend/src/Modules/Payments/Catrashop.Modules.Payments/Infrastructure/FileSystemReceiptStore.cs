using Catrashop.Modules.Payments.Application.Abstractions;
using Catrashop.Modules.Payments.Application.Configuration;
using Microsoft.Extensions.Options;

namespace Catrashop.Modules.Payments.Infrastructure;

/// <summary>
/// Stores transfer receipts as files under <see cref="PaymentsOptions.ReceiptsPath"/> (outside the repository). The
/// file name is generated here from the payment id and a random id, so nothing the client sends ever reaches the
/// file system; opening a key re-checks that it resolves inside the folder.
/// </summary>
public sealed class FileSystemReceiptStore : IReceiptStore
{
    private readonly string _root;

    public FileSystemReceiptStore(IOptions<PaymentsOptions> options)
    {
        var configured = options.Value.ReceiptsPath;
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Catrashop", "receipts")
            : configured);
    }

    public async Task<string> SaveAsync(Guid paymentId, byte[] content, string extension, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_root);
        var key = $"{paymentId:N}-{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(_root, key), content, ct);
        return key;
    }

    public Task<Stream?> OpenAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        Stream? stream = path is not null && File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true)
            : null;
        return Task.FromResult(stream);
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
        var full = Path.GetFullPath(Path.Combine(_root, key));
        return full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? full : null;
    }
}
