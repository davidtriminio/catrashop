using CommerceHub.Modules.Orders.Application.Abstractions;
using CommerceHub.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Orders.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _context;
    public OrderRepository(OrdersDbContext context) => _context = context;

    public void Add(Order order) => _context.Orders.Add(order);

    // Items and status history are owned collections and load with the order automatically.
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByCartIdAsync(Guid cartId, CancellationToken ct = default) =>
        _context.Orders.FirstOrDefaultAsync(o => o.CartId == cartId, ct);
}
