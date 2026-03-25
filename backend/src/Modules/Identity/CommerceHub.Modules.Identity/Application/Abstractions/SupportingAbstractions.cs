using CommerceHub.BuildingBlocks.Messaging;

namespace CommerceHub.Modules.Identity.Application.Abstractions;

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
