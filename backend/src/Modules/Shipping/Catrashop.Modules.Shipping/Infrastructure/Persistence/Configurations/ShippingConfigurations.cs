using Catrashop.Modules.Shipping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catrashop.Modules.Shipping.Infrastructure.Persistence.Configurations;

public sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
{
    public void Configure(EntityTypeBuilder<ShippingMethod> builder)
    {
        builder.ToTable("shipping_methods");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique().HasDatabaseName("ux_shipping_methods_code");
        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Cost).HasPrecision(18, 2);
        builder.Property(m => m.Currency).HasMaxLength(3).IsRequired();
    }
}

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");
        builder.HasKey(s => s.Id);

        // One shipment per order.
        builder.HasIndex(s => s.OrderId).IsUnique().HasDatabaseName("ux_shipments_order");
        builder.Property(s => s.TrackingNumber).HasMaxLength(40).IsRequired();
        builder.HasIndex(s => s.TrackingNumber).IsUnique().HasDatabaseName("ux_shipments_tracking");
        builder.Property(s => s.Carrier).HasMaxLength(100).IsRequired();
        builder.Property(s => s.MethodCode).HasMaxLength(40).IsRequired();
        builder.Property(s => s.Cost).HasPrecision(18, 2);
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(s => s.Destination, d =>
        {
            d.Property(x => x.RecipientName).HasMaxLength(150).IsRequired();
            d.Property(x => x.Line1).HasMaxLength(200).IsRequired();
            d.Property(x => x.Line2).HasMaxLength(200);
            d.Property(x => x.City).HasMaxLength(100).IsRequired();
            d.Property(x => x.Region).HasMaxLength(100);
            d.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
            d.Property(x => x.Country).HasMaxLength(2).IsRequired();
        });

        builder.OwnsMany(s => s.Events, e =>
        {
            e.ToTable("shipment_events");
            e.WithOwner().HasForeignKey(x => x.ShipmentId);
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(300);
        });

        builder.Metadata.FindNavigation(nameof(Shipment.Events))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ShippingZoneConfiguration : IEntityTypeConfiguration<ShippingZone>
{
    public void Configure(EntityTypeBuilder<ShippingZone> builder)
    {
        builder.ToTable("shipping_zones");
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(z => z.Code).IsUnique().HasDatabaseName("ux_shipping_zones_code");
        builder.Property(z => z.Name).HasMaxLength(100).IsRequired();
        builder.Property(z => z.Cost).HasPrecision(18, 2);
        builder.Property(z => z.Currency).HasMaxLength(3).IsRequired();

        builder.OwnsMany(z => z.Departments, d =>
        {
            d.ToTable("shipping_zone_departments");
            d.WithOwner().HasForeignKey(x => x.ZoneId);
            d.HasKey(x => new { x.ZoneId, x.DepartmentCode });
            d.Property(x => x.DepartmentCode).HasMaxLength(10).IsRequired();
            // A department is served by one zone only.
            d.HasIndex(x => x.DepartmentCode).IsUnique().HasDatabaseName("ux_shipping_zone_departments_department");
        });

        builder.Metadata.FindNavigation(nameof(ShippingZone.Departments))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PickupStoreConfiguration : IEntityTypeConfiguration<PickupStore>
{
    public void Configure(EntityTypeBuilder<PickupStore> builder)
    {
        builder.ToTable("pickup_stores");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Line1).HasMaxLength(200).IsRequired();
        builder.Property(s => s.City).HasMaxLength(100).IsRequired();
        builder.Property(s => s.DepartmentCode).HasMaxLength(10).IsRequired();
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Hours).HasMaxLength(200);
        builder.HasIndex(s => s.IsActive).HasDatabaseName("ix_pickup_stores_active");
    }
}
