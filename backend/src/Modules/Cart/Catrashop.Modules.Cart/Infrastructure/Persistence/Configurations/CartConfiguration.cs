using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catrashop.Modules.Cart.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Domain.Cart>
{
    public void Configure(EntityTypeBuilder<Domain.Cart> builder)
    {
        builder.ToTable("carts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Currency).HasMaxLength(3);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        // At most one Active cart per customer, enforced by a partial unique index.
        builder.HasIndex(c => c.CustomerId)
            .IsUnique()
            .HasFilter("status = 'Active'")
            .HasDatabaseName("ux_carts_customer_active");

        builder.OwnsMany(c => c.Items, item =>
        {
            item.ToTable("cart_items");
            item.WithOwner().HasForeignKey(i => i.CartId);
            item.HasKey(i => i.Id);
            item.Property(i => i.Sku).HasMaxLength(64).IsRequired();
            item.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            item.Property(i => i.UnitPrice).HasPrecision(18, 2);
            item.Property(i => i.Currency).HasMaxLength(3).IsRequired();
            item.HasIndex(i => new { i.CartId, i.VariantId }).HasDatabaseName("ix_cart_items_cart_variant");
        });

        builder.Metadata.FindNavigation(nameof(Domain.Cart.Items))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
