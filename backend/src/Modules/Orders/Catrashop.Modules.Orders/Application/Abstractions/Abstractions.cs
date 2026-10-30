using Catrashop.BuildingBlocks.Messaging;
using Catrashop.BuildingBlocks.Pagination;
using Catrashop.BuildingBlocks.Persistence;
using Catrashop.Modules.Orders.Application.Contracts;
using Catrashop.Modules.Orders.Domain;

namespace Catrashop.Modules.Orders.Application.Abstractions;

/// <summary>The Orders module's unit of work (module-specific marker over <see cref="IUnitOfWork"/>).</summary>
public interface IOrderUnitOfWork : IUnitOfWork;

/// <summary>Enqueues integration events into the Orders outbox within the current unit of work.</summary>
public interface IIntegrationEventPublisher
{
    void Enqueue(IIntegrationEvent integrationEvent, Guid? correlationId = null);
}

/// <summary>Write-side gateway for the <see cref="Order"/> aggregate.</summary>
public interface IOrderRepository
{
    void Add(Order order);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Finds an order already created from a cart — used to make checkout idempotent.</summary>
    Task<Order?> GetByCartIdAsync(Guid cartId, CancellationToken ct = default);
}

/// <summary>Read-side projections for order lists and detail.</summary>
public interface IOrderReadStore
{
    Task<PagedResult<OrderSummaryResponse>> ListForCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<OrderSummaryResponse>> SearchAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Order detail; when <paramref name="customerId"/> is provided, only that customer's order is returned.</summary>
    Task<OrderDetailResponse?> GetDetailAsync(Guid orderId, Guid? customerId, CancellationToken ct = default);

    /// <summary>A customer's own order looked up by its number (ownership-scoped); null if not found or not theirs.</summary>
    Task<Catrashop.Modules.Orders.Contracts.CustomerOrderView?> GetForCustomerByNumberAsync(
        Guid customerId, string orderNumber, CancellationToken ct = default);
}
