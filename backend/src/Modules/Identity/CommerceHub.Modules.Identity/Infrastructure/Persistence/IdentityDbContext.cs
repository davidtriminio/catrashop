using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Identity.Infrastructure.Persistence;

/// <summary>EF Core context for the Identity module. Owns the "identity" PostgreSQL schema.</summary>
public sealed class IdentityDbContext : ModuleDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    protected override string Schema => "identity";

    public DbSet<User> Users => Set<User>();
    public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();
}
