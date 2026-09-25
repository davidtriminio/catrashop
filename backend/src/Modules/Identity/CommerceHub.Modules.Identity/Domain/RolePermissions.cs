namespace CommerceHub.Modules.Identity.Domain;

/// <summary>
/// Maps each <see cref="Role"/> to the permissions it grants. Kept in code (not the database) at this
/// stage: the mapping is a policy decision that changes with releases, is trivially testable, and
/// avoids a join on every token issue. DB-driven permissions are noted as a future evolution in ADR-009.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<Role, IReadOnlySet<string>> Map =
        new Dictionary<Role, IReadOnlySet<string>>
        {
            [Role.Customer] = new HashSet<string>(),
            [Role.SupportAgent] = new HashSet<string>
            {
                Permissions.UsersRead,
                Permissions.OrdersReadAll
            },
            [Role.CatalogManager] = new HashSet<string>
            {
                Permissions.CatalogManage
            },
            [Role.InventoryManager] = new HashSet<string>
            {
                Permissions.InventoryManage
            },
            [Role.OrderManager] = new HashSet<string>
            {
                Permissions.OrdersReadAll,
                Permissions.OrdersManage
            },
            [Role.Administrator] = new HashSet<string>(Permissions.All)
        };

    /// <summary>The union of permissions granted by the supplied roles.</summary>
    public static IReadOnlySet<string> For(IEnumerable<Role> roles)
    {
        var result = new HashSet<string>();
        foreach (var role in roles)
        {
            if (Map.TryGetValue(role, out var permissions))
                result.UnionWith(permissions);
        }
        return result;
    }
}
