namespace CommerceHub.BuildingBlocks.Persistence;

/// <summary>
/// Commits a module's pending changes as a single atomic unit. Application/use-case code depends on
/// this abstraction rather than on EF's DbContext, keeping the domain and application layers free of
/// a persistence-technology reference.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
