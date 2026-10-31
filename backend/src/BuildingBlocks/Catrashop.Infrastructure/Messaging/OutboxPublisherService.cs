using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Catrashop.Infrastructure.Messaging;

/// <summary>
/// Periodically drains every module's outbox to the broker. Each cycle runs in its own DI scope, resolving the
/// per-module <see cref="IOutboxProcessor"/>s. The loop never throws out of itself — a bad cycle (e.g. the broker
/// is down) is logged and retried — so the app stays healthy whether or not RabbitMQ is running.
/// </summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(IServiceProvider services, IOptions<RabbitMqOptions> options, ILogger<OutboxPublisherService> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PublishIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var processors = scope.ServiceProvider.GetServices<IOutboxProcessor>();
                foreach (var processor in processors)
                {
                    try { await processor.ProcessAsync(stoppingToken); }
                    catch (Exception ex) { _logger.LogError(ex, "Outbox processor failed."); }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publish cycle failed.");
            }

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
