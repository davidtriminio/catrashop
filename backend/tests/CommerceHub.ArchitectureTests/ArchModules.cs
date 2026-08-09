using System.Reflection;
using CommerceHub.Modules.AI;
using CommerceHub.Modules.Cart;
using CommerceHub.Modules.Catalog;
using CommerceHub.Modules.Customers;
using CommerceHub.Modules.Identity;
using CommerceHub.Modules.Inventory;
using CommerceHub.Modules.Notifications;
using CommerceHub.Modules.Orders;
using CommerceHub.Modules.Payments;
using CommerceHub.Modules.Promotions;
using CommerceHub.Modules.Shipping;

namespace CommerceHub.ArchitectureTests;

/// <summary>
/// The set of business modules under inspection. Each entry pins a marker type from the module so the
/// module's assembly loads for dependency analysis, and records the root namespace used to derive the
/// per-layer namespaces the boundary rules assert against.
/// </summary>
public sealed record ArchModule(string Name, Assembly Assembly, string RootNamespace)
{
    public string Domain => $"{RootNamespace}.Domain";
    public string Application => $"{RootNamespace}.Application";
    public string Infrastructure => $"{RootNamespace}.Infrastructure";
    public string Api => $"{RootNamespace}.Api";

    /// <summary>Internal layers other modules must never reach into (Contracts is deliberately excluded).</summary>
    public string[] InternalNamespaces => [Domain, Application, Infrastructure, Api];
}

public static class ArchModules
{
    public const string ApiHostNamespace = "CommerceHub.Api";

    public static IReadOnlyList<ArchModule> All { get; } =
    [
        Module<IdentityModule>("Identity"),
        Module<CatalogModule>("Catalog"),
        Module<InventoryModule>("Inventory"),
        Module<CustomersModule>("Customers"),
        Module<CartModule>("Cart"),
        Module<PromotionsModule>("Promotions"),
        Module<OrdersModule>("Orders"),
        Module<PaymentsModule>("Payments"),
        Module<ShippingModule>("Shipping"),
        Module<NotificationsModule>("Notifications"),
        Module<AiModule>("AI"),
    ];

    private static ArchModule Module<TMarker>(string name)
    {
        var assembly = typeof(TMarker).Assembly;
        return new ArchModule(name, assembly, typeof(TMarker).Namespace!);
    }
}
