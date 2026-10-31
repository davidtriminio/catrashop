using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Inventory.Application.Abstractions;
using Catrashop.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Inventory.Infrastructure.Persistence;

/// <summary>EF Core context for the Inventory module. Owns the "inventory" PostgreSQL schema.</summary>
public sealed class InventoryDbContext : ModuleDbContext, IInventoryUnitOfWork
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    protected override string Schema => "inventory";

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
}
