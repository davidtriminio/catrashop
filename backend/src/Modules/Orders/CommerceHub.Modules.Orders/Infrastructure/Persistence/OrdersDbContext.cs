using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Orders.Application.Abstractions;
using CommerceHub.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Orders.Infrastructure.Persistence;

/// <summary>EF Core context for the Orders module. Owns the "orders" PostgreSQL schema.</summary>
public sealed class OrdersDbContext : ModuleDbContext, IOrderUnitOfWork
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    protected override string Schema => "orders";

    public DbSet<Order> Orders => Set<Order>();
}
