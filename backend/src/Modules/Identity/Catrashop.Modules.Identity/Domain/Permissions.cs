namespace Catrashop.Modules.Identity.Domain;

/// <summary>
/// The catalog of permission strings used for permission-based authorization. Endpoints require a
/// specific permission; a user holds a permission if any of their roles grants it. Using stable
/// dotted strings (module.resource.action) lets the set grow per module without changing the
/// authorization mechanism. See ADR-009.
/// </summary>
public static class Permissions
{
    // Identity / user administration
    public const string UsersRead = "identity.users.read";
    public const string UsersManageRoles = "identity.users.manage_roles";

    // Catalog administration (consumed by the Catalog module in a later phase)
    public const string CatalogManage = "catalog.products.manage";

    // Inventory administration
    public const string InventoryManage = "inventory.manage";

    // Order administration
    public const string OrdersReadAll = "orders.read_all";
    public const string OrdersManage = "orders.manage";

    // Promotions and notifications administration
    public const string PromotionsManage = "promotions.manage";
    public const string NotificationsManage = "notifications.manage";

    /// <summary>Every known permission — used to grant the Administrator role the full set.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        UsersRead,
        UsersManageRoles,
        CatalogManage,
        InventoryManage,
        OrdersReadAll,
        OrdersManage,
        PromotionsManage,
        NotificationsManage
    };
}
