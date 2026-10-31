using System.Reflection;
using Catrashop.Modules.Identity.Domain;
using Xunit;

namespace Catrashop.ArchitectureTests;

/// <summary>
/// Every endpoint guards itself with a permission string declared in its own module, while the Administrator role
/// is built from the Identity catalog. If a module adds a permission the catalog does not know, administrators get
/// a 403 on that module's admin API. This rule keeps the two in step.
/// </summary>
public sealed class PermissionCatalogTests
{
    private static IEnumerable<(string Module, string Field, string Value)> ModulePermissions() =>
        ArchModules.All
            .Where(m => m.Name != "Identity")
            .SelectMany(m => m.Assembly.GetTypes()
                .Where(t => t.IsAbstract && t.IsSealed && t.Namespace == m.Domain && t.Name.EndsWith("Permissions"))
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                    .Select(f => (m.Name, $"{t.Name}.{f.Name}", (string)f.GetRawConstantValue()!))));

    [Fact]
    public void Every_module_permission_is_in_the_identity_catalog_and_granted_to_administrators()
    {
        var declared = ModulePermissions().ToList();
        Assert.NotEmpty(declared);

        var administrator = RolePermissions.For([Role.Administrator]);
        var missing = declared.Where(p => !administrator.Contains(p.Value)).Select(p => $"{p.Module}: {p.Field} = '{p.Value}'").ToList();

        Assert.True(missing.Count == 0,
            "Administrators lack permissions declared by modules (add them to Identity.Domain.Permissions and Permissions.All): "
            + string.Join("; ", missing));
    }

    [Fact]
    public void Permission_strings_are_unique_and_dotted()
    {
        foreach (var value in Permissions.All)
            Assert.Matches(@"^[a-z_]+(\.[a-z_]+)+$", value);
        Assert.Equal(Permissions.All.Count, Permissions.All.Distinct().Count());
    }
}
