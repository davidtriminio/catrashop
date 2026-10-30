using Catrashop.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catrashop.Modules.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique().HasDatabaseName("ix_orders_number");

        builder.HasIndex(o => o.CustomerId).HasDatabaseName("ix_orders_customer");
        // One order per cart — enforces checkout idempotency at the database.
        builder.HasIndex(o => o.CartId).IsUnique().HasDatabaseName("ux_orders_cart");

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(o => o.Status).HasDatabaseName("ix_orders_status");
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.DiscountTotal).HasPrecision(18, 2);
        builder.Property(o => o.TaxTotal).HasPrecision(18, 2);
        builder.Property(o => o.ShippingTotal).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(o => o.DeliveryMode).HasConversion<string>().HasMaxLength(10).HasDefaultValue(DeliveryMode.Delivery);
        builder.Property(o => o.DepartmentCode).HasMaxLength(10);
        builder.Property(o => o.PickupStoreName).HasMaxLength(150);

        // Computed — not stored.
        builder.Ignore(o => o.Subtotal);
        builder.Ignore(o => o.GrandTotal);
        builder.Ignore(o => o.HoldsReservation);

        builder.OwnsOne(o => o.ShippingAddress, sa =>
        {
            sa.Property(a => a.RecipientName).HasMaxLength(150).IsRequired();
            sa.Property(a => a.Line1).HasMaxLength(200).IsRequired();
            sa.Property(a => a.Line2).HasMaxLength(200);
            sa.Property(a => a.City).HasMaxLength(100).IsRequired();
            sa.Property(a => a.Region).HasMaxLength(100);
            sa.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
            sa.Property(a => a.Country).HasMaxLength(2).IsRequired();
        });

        builder.OwnsMany(o => o.Items, item =>
        {
            item.ToTable("order_items");
            item.WithOwner().HasForeignKey(i => i.OrderId);
            item.HasKey(i => i.Id);
            item.Property(i => i.Sku).HasMaxLength(64).IsRequired();
            item.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            item.Property(i => i.UnitPrice).HasPrecision(18, 2);
        });

        builder.OwnsMany(o => o.StatusHistory, history =>
        {
            history.ToTable("order_status_history");
            history.WithOwner().HasForeignKey(h => h.OrderId);
            history.HasKey(h => h.Id);
            history.Property(h => h.Status).HasConversion<string>().HasMaxLength(20);
            history.Property(h => h.Note).HasMaxLength(300);
        });

        builder.Metadata.FindNavigation(nameof(Order.Items))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Order.StatusHistory))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
