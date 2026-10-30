namespace Catrashop.Modules.Orders.Domain;

/// <summary>Permission strings owned by the Orders module (granted to OrderManager/Administrator, and read to support).</summary>
public static class OrderPermissions
{
    public const string ReadAll = "orders.read_all";
    public const string Manage = "orders.manage";
}
