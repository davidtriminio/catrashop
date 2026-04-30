using CommerceHub.Modules.Customers.Application.Abstractions;
using CommerceHub.Modules.Customers.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Customers.Infrastructure.Persistence;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly CustomersDbContext _context;
    public CustomerRepository(CustomersDbContext context) => _context = context;

    public void Add(Customer customer) => _context.Customers.Add(customer);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        _context.Customers.AnyAsync(c => c.Id == id, ct);

    // Addresses and wishlist are owned collections and load with the customer automatically.
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
}
