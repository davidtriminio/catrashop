namespace Catrashop.Modules.Catalog.Domain;

/// <summary>
/// Permission strings owned by the Catalog module. The value matches the entry the Identity module's
/// role map grants to CatalogManager/Administrator — the string is the shared contract between them.
/// </summary>
public static class CatalogPermissions
{
    public const string Manage = "catalog.products.manage";
}
