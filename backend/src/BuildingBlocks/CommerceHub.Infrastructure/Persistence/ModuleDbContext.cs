using CommerceHub.BuildingBlocks.Persistence;
using CommerceHub.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Infrastructure.Persistence;

/// <summary>
/// Base <see cref="DbContext"/> every module derives from. It gives each module its own PostgreSQL
/// schema (so tables never collide and boundaries stay visible in the database), an <c>outbox_messages</c>
/// table wired into the same transaction, and a shared <see cref="IUnitOfWork"/> implementation.
/// Derived contexts only declare their <see cref="Schema"/> and map their own entities.
/// </summary>
public abstract class ModuleDbContext : DbContext, IUnitOfWork
{
    protected ModuleDbContext(DbContextOptions options) : base(options) { }

    /// <summary>The dedicated PostgreSQL schema for this module (e.g. "catalog", "identity").</summary>
    protected abstract string Schema { get; }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // The outbox lives in every module's schema and transaction.
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        // Then the module's own entity configurations (from the derived context's assembly).
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
