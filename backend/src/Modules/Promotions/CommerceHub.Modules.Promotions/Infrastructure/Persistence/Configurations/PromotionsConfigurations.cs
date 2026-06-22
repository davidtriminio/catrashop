using CommerceHub.Modules.Promotions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.Modules.Promotions.Infrastructure.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique().HasDatabaseName("ux_coupons_code");
        builder.Property(c => c.Name).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.DiscountType).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.DiscountValue).HasPrecision(18, 2);
        builder.Property(c => c.Currency).HasMaxLength(3);
        builder.Property(c => c.MinimumSubtotal).HasPrecision(18, 2);
        builder.Property(c => c.MaxDiscountAmount).HasPrecision(18, 2);
    }
}

public sealed class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.ToTable("coupon_redemptions");
        builder.HasKey(r => r.Id);

        // One redemption per order reference — the idempotency guard for checkout retries.
        builder.HasIndex(r => r.Reference).IsUnique().HasDatabaseName("ux_coupon_redemptions_reference");
        builder.HasIndex(r => new { r.CouponId, r.CustomerId }).HasDatabaseName("ix_coupon_redemptions_coupon_customer");

        builder.Property(r => r.CouponCode).HasMaxLength(40).IsRequired();
        builder.Property(r => r.DiscountAmount).HasPrecision(18, 2);
        builder.Property(r => r.Currency).HasMaxLength(3).IsRequired();

        builder.HasOne<Coupon>().WithMany().HasForeignKey(r => r.CouponId).OnDelete(DeleteBehavior.Restrict);
    }
}
