using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Catalog.Application.Abstractions;
using CommerceHub.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Catalog.Infrastructure.Persistence;

/// <summary>EF Core context for the Catalog module. Owns the "catalog" PostgreSQL schema.</summary>
public sealed class CatalogDbContext : ModuleDbContext, ICatalogUnitOfWork
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    protected override string Schema => "catalog";

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
}
