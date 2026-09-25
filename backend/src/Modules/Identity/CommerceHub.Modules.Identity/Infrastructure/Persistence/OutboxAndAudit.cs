using System.Text.Json;
using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.Infrastructure.Outbox;
using CommerceHub.Modules.Identity.Application.Abstractions;
using CommerceHub.Modules.Identity.Domain;

namespace CommerceHub.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Writes integration events into the module's outbox table within the current unit of work. The event
/// type's full name is stored so the publisher (Phase 15) can resolve and deserialize it.
/// </summary>
public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IdentityDbContext _context;

    public OutboxIntegrationEventPublisher(IdentityDbContext context) => _context = context;

    public void Enqueue(IIntegrationEvent integrationEvent, Guid? correlationId = null)
    {
        var message = new OutboxMessage(
            Guid.NewGuid(),
            integrationEvent.GetType().FullName!,
            JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            integrationEvent.OccurredOnUtc,
            correlationId);

        _context.OutboxMessages.Add(message);
    }
}

/// <summary>Appends security-audit entries to the current unit of work.</summary>
public sealed class SecurityAuditLogger : ISecurityAuditLogger
{
    private readonly IdentityDbContext _context;
    private readonly TimeProvider _clock;

    public SecurityAuditLogger(IdentityDbContext context, TimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public void Record(string eventType, Guid? userId, string? detail = null) =>
        _context.SecurityAuditLogs.Add(
            new SecurityAuditLog(Guid.NewGuid(), eventType, userId, detail, _clock.GetUtcNow()));
}
