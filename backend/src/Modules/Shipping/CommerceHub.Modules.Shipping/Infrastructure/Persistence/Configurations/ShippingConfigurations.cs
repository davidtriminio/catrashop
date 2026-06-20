using CommerceHub.Modules.Shipping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.Modules.Shipping.Infrastructure.Persistence.Configurations;

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
