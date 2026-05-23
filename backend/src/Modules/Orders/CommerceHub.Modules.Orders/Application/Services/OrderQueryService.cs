using CommerceHub.BuildingBlocks.Pagination;
using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Orders.Application.Abstractions;
using CommerceHub.Modules.Orders.Application.Contracts;
using CommerceHub.Modules.Orders.Domain;

namespace CommerceHub.Modules.Orders.Application.Services;

/// <summary>Read queries over orders — the customer's own, and (for staff) all orders.</summary>
public sealed class OrderQueryService
{
    private readonly IOrderReadStore _readStore;

    public OrderQueryService(IOrderReadStore readStore) => _readStore = readStore;

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
