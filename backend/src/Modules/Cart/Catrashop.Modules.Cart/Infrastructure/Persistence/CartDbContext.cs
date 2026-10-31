using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Cart.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Cart.Infrastructure.Persistence;

/// <summary>EF Core context for the Cart module. Owns the "cart" PostgreSQL schema.</summary>
public sealed class CartDbContext : ModuleDbContext, ICartUnitOfWork
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options) { }

    protected override string Schema => "cart";

    public DbSet<Domain.Cart> Carts => Set<Domain.Cart>();
}
