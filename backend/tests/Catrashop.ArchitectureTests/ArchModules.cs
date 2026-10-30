using System.Reflection;
using Catrashop.Modules.AI;
using Catrashop.Modules.Cart;
using Catrashop.Modules.Catalog;
using Catrashop.Modules.Customers;
using Catrashop.Modules.Identity;
using Catrashop.Modules.Inventory;
using Catrashop.Modules.Notifications;
using Catrashop.Modules.Orders;
using Catrashop.Modules.Payments;
using Catrashop.Modules.Promotions;
using Catrashop.Modules.Shipping;

namespace Catrashop.ArchitectureTests;

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
    public const string ApiHostNamespace = "Catrashop.Api";

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
