using CommerceHub.Modules.Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.Modules.Customers.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id); // = user id (no generation)
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Email).HasMaxLength(256).IsRequired();
        builder.Property(c => c.FirstName).HasMaxLength(100);
        builder.Property(c => c.LastName).HasMaxLength(100);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.PreferredLanguage).HasMaxLength(10);
        builder.HasIndex(c => c.Email).HasDatabaseName("ix_customers_email");

        builder.OwnsMany(c => c.Addresses, address =>
        {
            address.ToTable("addresses");
            address.WithOwner().HasForeignKey(a => a.CustomerId);
            address.HasKey(a => a.Id);
            address.Property(a => a.Label).HasMaxLength(50).IsRequired();
            address.Property(a => a.RecipientName).HasMaxLength(150).IsRequired();
            address.Property(a => a.Line1).HasMaxLength(200).IsRequired();
            address.Property(a => a.Line2).HasMaxLength(200);
            address.Property(a => a.City).HasMaxLength(100).IsRequired();
            address.Property(a => a.Region).HasMaxLength(100);
            address.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
            address.Property(a => a.Country).HasMaxLength(2).IsRequired();
            address.Property(a => a.Phone).HasMaxLength(30);
        });

        builder.OwnsMany(c => c.WishlistItems, wish =>
        {
            wish.ToTable("wishlist_items");
            wish.WithOwner().HasForeignKey(w => w.CustomerId);
            wish.HasKey(w => w.Id);
            // A product appears at most once in a customer's wishlist.
            wish.HasIndex(w => new { w.CustomerId, w.ProductId }).IsUnique().HasDatabaseName("ix_wishlist_customer_product");
        });

        builder.Metadata.FindNavigation(nameof(Customer.Addresses))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Customer.WishlistItems))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
