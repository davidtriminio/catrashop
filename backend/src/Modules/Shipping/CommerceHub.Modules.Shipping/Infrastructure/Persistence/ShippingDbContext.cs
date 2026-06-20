using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Shipping.Application.Abstractions;
using CommerceHub.Modules.Shipping.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Shipping.Infrastructure.Persistence;

/// <summary>EF Core context for the Shipping module. Owns the "shipping" PostgreSQL schema.</summary>
public sealed class ShippingDbContext : ModuleDbContext, IShippingUnitOfWork
{
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options) { }

    protected override string Schema => "shipping";

    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
}
