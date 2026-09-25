using System.Diagnostics;
using CommerceHub.BuildingBlocks.Observability;
using CommerceHub.Infrastructure.Observability;
using CommerceHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommerceHub.Infrastructure.Messaging;

/// <summary>Drains one module's outbox to the broker. One implementation is registered per module DbContext.</summary>
public interface IOutboxProcessor
{
    Task<int> ProcessAsync(CancellationToken ct = default);
}

/// <summary>
/// Publishes a module's unprocessed outbox rows (oldest first) to the broker and marks them processed in the
/// same context, so a message is delivered at least once and never lost. On a publish failure the row is marked
/// (retry count + error) and the batch stops — it is retried on the next cycle.
/// </summary>
public sealed class OutboxProcessor<TContext> : IOutboxProcessor where TContext : ModuleDbContext
{
    private readonly TContext _context;
    private readonly IEventBus _bus;
    private readonly TimeProvider _clock;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxProcessor<TContext>> _logger;

    public OutboxProcessor(TContext context, IEventBus bus, TimeProvider clock, IOptions<RabbitMqOptions> options, ILogger<OutboxProcessor<TContext>> logger)
    {
        _context = context;
        _bus = bus;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ProcessAsync(CancellationToken ct = default)
    {
        var pending = await _context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_options.PublishBatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return 0;

        using var activity = CommerceHubTelemetry.ActivitySource.StartActivity("outbox.drain", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("commercehub.module", typeof(TContext).Name);
        activity?.SetTag("commercehub.outbox.pending", pending.Count);

        var published = 0;
        foreach (var message in pending)
        {
            try
            {
                await _bus.PublishAsync(message.Type, message.Content, message.Id, message.CorrelationId, ct);
                message.MarkProcessed(_clock.GetUtcNow());
                published++;
                MessagingMetrics.OutboxPublished.Add(1, new KeyValuePair<string, object?>("event.type", message.Type));
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                MessagingMetrics.OutboxFailed.Add(1, new KeyValuePair<string, object?>("event.type", message.Type));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogWarning(ex, "Failed to publish outbox message {MessageId} ({Type}); will retry.", message.Id, message.Type);
                break; // broker likely unavailable — stop and retry the batch next cycle
            }
        }

        activity?.SetTag("commercehub.outbox.published", published);

        await _context.SaveChangesAsync(ct);
        return published;
    }
}
