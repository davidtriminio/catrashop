using CommerceHub.Modules.Cart.Application.Abstractions;
using CommerceHub.Modules.Cart.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Cart.Infrastructure.Persistence;

public sealed class CartRepository : ICartRepository
{
    private readonly CartDbContext _context;
    public CartRepository(CartDbContext context) => _context = context;

    public void Add(Domain.Cart cart) => _context.Carts.Add(cart);

    // Items are an owned collection and load with the cart automatically.
    public Task<Domain.Cart?> GetActiveByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == CartStatus.Active, ct);

    public Task<Domain.Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default) =>
        _context.Carts.FirstOrDefaultAsync(c => c.Id == cartId, ct);
}
