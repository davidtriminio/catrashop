using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.BuildingBlocks.Persistence;

namespace CommerceHub.Modules.Identity.Application.Abstractions;

/// <summary>
/// The Identity module's unit of work. A module-specific marker (over the shared <see cref="IUnitOfWork"/>)
/// so that, in the single shared DI container, each module's services resolve *their own* DbContext rather
/// than whichever module registered a generic IUnitOfWork last.
/// </summary>
public interface IIdentityUnitOfWork : IUnitOfWork;

/// <summary>
/// Enqueues an integration event into this module's outbox, within the current unit of work. The
/// event is published to the broker later by the outbox worker (ADR-005), so the caller only needs
/// to enqueue and save.
/// </summary>
public interface IIntegrationEventPublisher
{
    void Enqueue(IIntegrationEvent integrationEvent, Guid? correlationId = null);
}

/// <summary>Records security-relevant events (logins, resets, lockouts) to a durable audit trail.</summary>
public interface ISecurityAuditLogger
{
    void Record(string eventType, Guid? userId, string? detail = null);
}
