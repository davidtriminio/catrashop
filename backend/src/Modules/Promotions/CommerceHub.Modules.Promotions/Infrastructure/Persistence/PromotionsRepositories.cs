using CommerceHub.Modules.Promotions.Application.Abstractions;
using CommerceHub.Modules.Promotions.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Promotions.Infrastructure.Persistence;

public sealed class CouponRepository : ICouponRepository
{
    private readonly PromotionsDbContext _context;
    public CouponRepository(PromotionsDbContext context) => _context = context;

    public void Add(Coupon coupon) => _context.Coupons.Add(coupon);

    public Task<Coupon?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _context.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        _context.Coupons.AnyAsync(c => c.Code == code, ct);

    public async Task<IReadOnlyList<Coupon>> ListAsync(CancellationToken ct = default) =>
        await _context.Coupons.OrderByDescending(c => c.CreatedAtUtc).ToListAsync(ct);
}

public sealed class CouponRedemptionRepository : ICouponRedemptionRepository
{
    private readonly PromotionsDbContext _context;
    public CouponRedemptionRepository(PromotionsDbContext context) => _context = context;

    public void Add(CouponRedemption redemption) => _context.CouponRedemptions.Add(redemption);

    public Task<CouponRedemption?> GetByReferenceAsync(Guid reference, CancellationToken ct = default) =>
        _context.CouponRedemptions.FirstOrDefaultAsync(r => r.Reference == reference, ct);

    public Task<int> CountForCustomerAsync(Guid couponId, Guid customerId, CancellationToken ct = default) =>
        _context.CouponRedemptions.CountAsync(
            r => r.CouponId == couponId && r.CustomerId == customerId && r.ReleasedAtUtc == null, ct);
}
