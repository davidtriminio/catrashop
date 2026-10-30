using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Shipping.Application.Abstractions;
using Catrashop.Modules.Shipping.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Shipping.Infrastructure.Persistence;

/// <summary>EF Core context for the Shipping module. Owns the "shipping" PostgreSQL schema.</summary>
public sealed class ShippingDbContext : ModuleDbContext, IShippingUnitOfWork
{
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options) { }

    protected override string Schema => "shipping";

    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShippingZone> ShippingZones => Set<ShippingZone>();
    public DbSet<PickupStore> PickupStores => Set<PickupStore>();
}
