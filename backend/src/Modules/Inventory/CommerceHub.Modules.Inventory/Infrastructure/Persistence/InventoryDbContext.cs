using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Inventory.Application.Abstractions;
using CommerceHub.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Inventory.Infrastructure.Persistence;

/// <summary>EF Core context for the Inventory module. Owns the "inventory" PostgreSQL schema.</summary>
public sealed class InventoryDbContext : ModuleDbContext, IInventoryUnitOfWork
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    protected override string Schema => "inventory";

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
}
