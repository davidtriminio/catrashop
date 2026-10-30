using Catrashop.Modules.Customers.Application.Abstractions;
using Catrashop.Modules.Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Catrashop.Modules.Customers.Infrastructure.Persistence;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly CustomersDbContext _context;
    public CustomerRepository(CustomersDbContext context) => _context = context;

    public async Task<bool> TryAddAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Add(customer);
        try
        {
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Lost the race: stop tracking our copy so later queries/saves on this scope see the committed row.
            _context.Entry(customer).State = EntityState.Detached;
            return false;
        }
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        _context.Customers.AnyAsync(c => c.Id == id, ct);

    // Addresses and wishlist are owned collections and load with the customer automatically.
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
}
