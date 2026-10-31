namespace Catrashop.Modules.Identity.Domain;

/// <summary>
/// The fixed set of roles in Catrashop. Roles are coarse job functions; fine-grained access is
/// expressed as <see cref="Permissions"/> that each role grants (see <see cref="RolePermissions"/>).
/// </summary>
public enum Role
{
    Customer = 0,
    SupportAgent = 1,
    CatalogManager = 2,
    InventoryManager = 3,
    OrderManager = 4,
    Administrator = 5
}
