namespace Catrashop.Modules.Catalog.Application.Configuration;

/// <summary>Settings of the Catalog module (configuration section "Catalog").</summary>
public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    /// <summary>
    /// Folder where uploaded product images are stored. Defaults to a "product-images" folder under the user's
    /// local application data; set it to a persistent volume in production.
    /// </summary>
    public string? ImagesPath { get; set; }

    /// <summary>Largest image accepted, in bytes (default 5 MB).</summary>
    public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
}
