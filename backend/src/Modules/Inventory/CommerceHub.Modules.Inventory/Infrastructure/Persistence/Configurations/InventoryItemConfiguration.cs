using CommerceHub.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.Modules.Inventory.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Sku).HasMaxLength(64).IsRequired();
        builder.HasIndex(i => i.Sku).IsUnique().HasDatabaseName("ix_inventory_items_sku");
        builder.Property(i => i.QuantityOnHand);
        builder.Property(i => i.QuantityReserved);
        builder.Property(i => i.LowStockThreshold);
        builder.Property(i => i.LowStockNotified);

        builder.Ignore(i => i.AvailableQuantity); // computed

        builder.OwnsMany(i => i.Movements, movement =>
        {
            movement.ToTable("stock_movements");
            movement.WithOwner().HasForeignKey(m => m.ItemId);
            movement.HasKey(m => m.Id);
            movement.Property(m => m.Type).HasConversion<string>().HasMaxLength(20);
            movement.Property(m => m.Reason).HasMaxLength(300);
            movement.HasIndex(m => m.Reference).HasDatabaseName("ix_stock_movements_reference");
            movement.HasIndex(m => m.OccurredAtUtc).HasDatabaseName("ix_stock_movements_occurred");
        });

        builder.OwnsMany(i => i.Reservations, reservation =>
        {
            reservation.ToTable("reservations");
            reservation.WithOwner().HasForeignKey(r => r.ItemId);
            reservation.HasKey(r => r.Id);
            reservation.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            reservation.HasIndex(r => r.Reference).HasDatabaseName("ix_reservations_reference");
            // A reference holds a SKU at most once — supports idempotent reservation.
            reservation.HasIndex(r => new { r.ItemId, r.Reference }).IsUnique().HasDatabaseName("ix_reservations_item_reference");
        });

        builder.Metadata.FindNavigation(nameof(InventoryItem.Movements))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(InventoryItem.Reservations))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
