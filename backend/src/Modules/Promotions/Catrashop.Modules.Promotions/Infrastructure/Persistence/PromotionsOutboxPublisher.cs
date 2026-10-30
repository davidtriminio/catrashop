using System.Text.Json;
using Catrashop.BuildingBlocks.Messaging;
using Catrashop.Infrastructure.Outbox;
using Catrashop.Modules.Promotions.Application.Abstractions;

namespace Catrashop.Modules.Promotions.Infrastructure.Persistence;

/// <summary>Writes Promotions integration events to the outbox within the current unit of work.</summary>
public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly PromotionsDbContext _context;

    public OutboxIntegrationEventPublisher(PromotionsDbContext context) => _context = context;

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
