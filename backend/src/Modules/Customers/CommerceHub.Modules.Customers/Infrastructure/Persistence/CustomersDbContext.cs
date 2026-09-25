using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Customers.Application.Abstractions;
using CommerceHub.Modules.Customers.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Customers.Infrastructure.Persistence;

/// <summary>EF Core context for the Customers module. Owns the "customers" PostgreSQL schema.</summary>
public sealed class CustomersDbContext : ModuleDbContext, ICustomerUnitOfWork
{
    public CustomersDbContext(DbContextOptions<CustomersDbContext> options) : base(options) { }

    protected override string Schema => "customers";

    public DbSet<Customer> Customers => Set<Customer>();
}
