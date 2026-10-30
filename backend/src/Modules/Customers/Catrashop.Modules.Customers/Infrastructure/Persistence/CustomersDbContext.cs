using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Customers.Application.Abstractions;
using Catrashop.Modules.Customers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Customers.Infrastructure.Persistence;

/// <summary>EF Core context for the Customers module. Owns the "customers" PostgreSQL schema.</summary>
public sealed class CustomersDbContext : ModuleDbContext, ICustomerUnitOfWork
{
    public CustomersDbContext(DbContextOptions<CustomersDbContext> options) : base(options) { }

    protected override string Schema => "customers";

    public DbSet<Customer> Customers => Set<Customer>();
}
