using Catrashop.BuildingBlocks.Messaging;
using Catrashop.BuildingBlocks.Persistence;
using Catrashop.Modules.Promotions.Domain;

namespace Catrashop.Modules.Promotions.Application.Abstractions;

/// <summary>The Promotions module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface IPromotionsUnitOfWork : IUnitOfWork;

/// <summary>Enqueues integration events into the Promotions outbox within the current unit of work.</summary>
public interface IIntegrationEventPublisher
{
    void Enqueue(IIntegrationEvent integrationEvent, Guid? correlationId = null);
}

public interface ICouponRepository
{
    void Add(Coupon coupon);
    Task<Coupon?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Coupon>> ListAsync(CancellationToken ct = default);
}

public interface ICouponRedemptionRepository
{
    void Add(CouponRedemption redemption);

    /// <summary>The redemption already recorded for an order reference, if any (idempotency lookup).</summary>
    Task<CouponRedemption?> GetByReferenceAsync(Guid reference, CancellationToken ct = default);

    /// <summary>How many times a customer has an active (non-released) redemption of a coupon.</summary>
    Task<int> CountForCustomerAsync(Guid couponId, Guid customerId, CancellationToken ct = default);
}
