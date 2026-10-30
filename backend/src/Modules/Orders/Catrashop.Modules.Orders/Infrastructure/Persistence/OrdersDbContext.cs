using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Orders.Application.Abstractions;
using Catrashop.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Orders.Infrastructure.Persistence;

/// <summary>EF Core context for the Orders module. Owns the "orders" PostgreSQL schema.</summary>
public sealed class OrdersDbContext : ModuleDbContext, IOrderUnitOfWork
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    protected override string Schema => "orders";

    public DbSet<Order> Orders => Set<Order>();
}
