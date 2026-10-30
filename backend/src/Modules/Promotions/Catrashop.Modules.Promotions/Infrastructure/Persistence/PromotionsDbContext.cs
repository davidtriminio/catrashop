using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Promotions.Application.Abstractions;
using Catrashop.Modules.Promotions.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Promotions.Infrastructure.Persistence;

/// <summary>EF Core context for the Promotions module. Owns the "promotions" PostgreSQL schema.</summary>
public sealed class PromotionsDbContext : ModuleDbContext, IPromotionsUnitOfWork
{
    public PromotionsDbContext(DbContextOptions<PromotionsDbContext> options) : base(options) { }

    protected override string Schema => "promotions";

    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
}
