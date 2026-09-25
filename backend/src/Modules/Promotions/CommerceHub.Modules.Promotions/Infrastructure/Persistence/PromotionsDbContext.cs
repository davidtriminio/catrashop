using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Promotions.Application.Abstractions;
using CommerceHub.Modules.Promotions.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Promotions.Infrastructure.Persistence;

/// <summary>EF Core context for the Promotions module. Owns the "promotions" PostgreSQL schema.</summary>
public sealed class PromotionsDbContext : ModuleDbContext, IPromotionsUnitOfWork
{
    public PromotionsDbContext(DbContextOptions<PromotionsDbContext> options) : base(options) { }

    protected override string Schema => "promotions";

    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
}
