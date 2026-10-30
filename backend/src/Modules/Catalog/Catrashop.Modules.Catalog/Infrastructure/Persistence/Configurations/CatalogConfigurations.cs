using Catrashop.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catrashop.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(170).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("ix_categories_slug");

        // Self-referencing hierarchy; restrict delete so a category with children can't be orphaned.
        builder.HasOne<Category>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.ParentId).HasDatabaseName("ix_categories_parent");
    }
}

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Slug).HasMaxLength(170).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(2000);
        builder.HasIndex(b => b.Slug).IsUnique().HasDatabaseName("ix_brands_slug");
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(220).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(8000).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("ix_products_slug");
        builder.HasIndex(p => p.Status).HasDatabaseName("ix_products_status");

        // Referential integrity within the catalog schema.
        builder.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Brand>().WithMany().HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.SetNull);

        builder.OwnsMany(p => p.Variants, variant =>
        {
            variant.ToTable("product_variants");
            variant.WithOwner().HasForeignKey(v => v.ProductId);
            variant.HasKey(v => v.Id);
            variant.Property(v => v.Sku).HasMaxLength(64).IsRequired();
            variant.Property(v => v.Name).HasMaxLength(150).IsRequired();
            variant.Property(v => v.Price).HasPrecision(18, 2);
            variant.Property(v => v.Currency).HasMaxLength(3).IsRequired();
            // SKU is globally unique across the whole catalog.
            variant.HasIndex(v => v.Sku).IsUnique().HasDatabaseName("ix_product_variants_sku");
        });

        builder.OwnsMany(p => p.Attributes, attribute =>
        {
            attribute.ToTable("product_attributes");
            attribute.WithOwner().HasForeignKey(a => a.ProductId);
            attribute.HasKey(a => a.Id);
            attribute.Property(a => a.Name).HasMaxLength(100).IsRequired();
            attribute.Property(a => a.Value).HasMaxLength(500).IsRequired();
        });

        builder.OwnsMany(p => p.Images, image =>
        {
            image.ToTable("product_images");
            image.WithOwner().HasForeignKey(i => i.ProductId);
            image.HasKey(i => i.Id);
            image.Property(i => i.Url).HasMaxLength(2048).IsRequired();
            image.Property(i => i.AltText).HasMaxLength(300);
        });

        builder.Metadata.FindNavigation(nameof(Product.Variants))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Product.Attributes))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Product.Images))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
