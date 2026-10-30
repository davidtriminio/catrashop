using Catrashop.BuildingBlocks.Pagination;
using Catrashop.Modules.Orders.Application.Abstractions;
using Catrashop.Modules.Orders.Application.Contracts;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Orders.Infrastructure.Persistence;

/// <summary>Projection-based read model for order lists and detail.</summary>
public sealed class OrderReadStore : IOrderReadStore
{
    private readonly OrdersDbContext _context;
    public OrderReadStore(OrdersDbContext context) => _context = context;

    public Task<PagedResult<OrderSummaryResponse>> ListForCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken ct = default) =>
        PageSummariesAsync(_context.Orders.Where(o => o.CustomerId == customerId), page, pageSize, ct);

    public Task<PagedResult<OrderSummaryResponse>> SearchAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Orders.AsQueryable();
        if (status is { } s)
            query = query.Where(o => o.Status == s);
        return PageSummariesAsync(query, page, pageSize, ct);
    }

    private static async Task<PagedResult<OrderSummaryResponse>> PageSummariesAsync(
        IQueryable<Order> query, int page, int pageSize, CancellationToken ct)
    {
        var request = new PageRequest(page, pageSize);
        var total = await query.LongCountAsync(ct);

        var rows = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Currency,
                Subtotal = o.Items.Sum(i => i.UnitPrice * i.Quantity),
                o.DiscountTotal,
                o.TaxTotal,
                o.ShippingTotal,
                ItemCount = o.Items.Sum(i => i.Quantity),
                o.CreatedAtUtc
            })
            .ToListAsync(ct);

        var items = rows
            .Select(o => new OrderSummaryResponse(o.Id, o.OrderNumber, o.Status.ToString(), o.Currency,
                o.Subtotal - o.DiscountTotal + o.TaxTotal + o.ShippingTotal, o.ItemCount, o.CreatedAtUtc))
            .ToArray();

        return new PagedResult<OrderSummaryResponse>(items, request.Page, request.PageSize, total);
    }

    public async Task<CustomerOrderView?> GetForCustomerByNumberAsync(Guid customerId, string orderNumber, CancellationToken ct = default)
    {
        var normalized = orderNumber.Trim();
        var row = await _context.Orders
            .Where(o => o.CustomerId == customerId && o.OrderNumber == normalized)
            .Select(o => new
            {
                o.OrderNumber,
                o.Status,
                o.Currency,
                Subtotal = o.Items.Sum(i => i.UnitPrice * i.Quantity),
                o.DiscountTotal,
                o.TaxTotal,
                o.ShippingTotal,
                o.CreatedAtUtc
            })
            .FirstOrDefaultAsync(ct);

        return row is null
            ? null
            : new CustomerOrderView(row.OrderNumber, row.Status.ToString(), row.Subtotal - row.DiscountTotal + row.TaxTotal + row.ShippingTotal, row.Currency, row.CreatedAtUtc);
    }

    public async Task<OrderDetailResponse?> GetDetailAsync(Guid orderId, Guid? customerId, CancellationToken ct = default)
    {
        // Read-only detail projection (owned Items/StatusHistory/ShippingAddress load with the aggregate);
        // no tracking needed on this display path.
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && (customerId == null || o.CustomerId == customerId), ct);
        if (order is null)
            return null;

        var a = order.ShippingAddress;
        return new OrderDetailResponse(
            order.Id, order.OrderNumber, order.CustomerId, order.Status.ToString(), order.Currency,
            order.Subtotal, order.DiscountTotal, order.TaxTotal, order.ShippingTotal, order.GrandTotal,
            order.DeliveryMode.ToString(), order.PickupStoreId, order.PickupStoreName,
            new ShippingAddressRequest(a.RecipientName, a.Line1, a.Line2, a.City, a.Region, a.PostalCode, a.Country, order.DepartmentCode),
            order.Items
                .Select(i => new OrderItemResponse(i.Id, i.ProductId, i.VariantId, i.Sku, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal))
                .ToArray(),
            order.StatusHistory
                .OrderBy(h => h.OccurredAtUtc)
                .Select(h => new OrderStatusHistoryResponse(h.Status.ToString(), h.Note, h.OccurredAtUtc))
                .ToArray(),
            order.CreatedAtUtc);
    }
}
