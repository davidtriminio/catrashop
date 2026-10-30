using Catrashop.BuildingBlocks.Pagination;
using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Orders.Application.Abstractions;
using Catrashop.Modules.Orders.Application.Contracts;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Orders.Domain;

namespace Catrashop.Modules.Orders.Application.Services;

/// <summary>Read queries over orders — the customer's own, and (for staff) all orders. Also implements the
/// public <see cref="IOrderQueries"/> contract used by other modules (ownership-scoped by customer id).</summary>
public sealed class OrderQueryService : IOrderQueries
{
    private readonly IOrderReadStore _readStore;

    public OrderQueryService(IOrderReadStore readStore) => _readStore = readStore;

    public Task<CustomerOrderView?> GetForCustomerByNumberAsync(Guid customerId, string orderNumber, CancellationToken ct = default) =>
        _readStore.GetForCustomerByNumberAsync(customerId, orderNumber, ct);

    public Task<PagedResult<OrderSummaryResponse>> ListMineAsync(Guid customerId, int page, int pageSize, CancellationToken ct = default) =>
        _readStore.ListForCustomerAsync(customerId, page, pageSize, ct);

    public async Task<Result<OrderDetailResponse>> GetMineAsync(Guid customerId, Guid orderId, CancellationToken ct = default)
    {
        var order = await _readStore.GetDetailAsync(orderId, customerId, ct);
        return order is null ? OrderErrors.NotFound : order;
    }

    public Task<PagedResult<OrderSummaryResponse>> SearchAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default) =>
        _readStore.SearchAsync(status, page, pageSize, ct);

    public async Task<Result<OrderDetailResponse>> GetAnyAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _readStore.GetDetailAsync(orderId, null, ct);
        return order is null ? OrderErrors.NotFound : order;
    }
}
