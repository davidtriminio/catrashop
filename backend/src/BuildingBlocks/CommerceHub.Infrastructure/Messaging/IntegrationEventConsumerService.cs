using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommerceHub.Infrastructure.Messaging;

/// <summary>
/// Subscribes to the topic exchange for every registered event type and dispatches each message to its
/// handlers in a fresh DI scope. Delivery is manual-ack: a handled message is acked, a failed one is nacked and
/// requeued (handlers are idempotent, so redelivery is safe). Connection setup retries so the app runs whether
/// or not RabbitMQ is up, and RabbitMQ's automatic recovery reconnects dropped connections.
/// </summary>
public sealed class IntegrationEventConsumerService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IntegrationEventDispatcher _dispatcher;
    private readonly IntegrationEventTypeRegistry _registry;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<IntegrationEventConsumerService> _logger;

    public IntegrationEventConsumerService(
        IServiceProvider services,
        IntegrationEventDispatcher dispatcher,
        IntegrationEventTypeRegistry registry,
        IOptions<RabbitMqOptions> options,
        ILogger<IntegrationEventConsumerService> logger)
    {
        _services = services;
        _dispatcher = dispatcher;
        _registry = registry;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ consumer unavailable; retrying in 5s.");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        foreach (var routingKey in _registry.Names)
            await channel.QueueBindAsync(_options.QueueName, _options.Exchange, routingKey, cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var type = ea.RoutingKey;
            var content = Encoding.UTF8.GetString(ea.Body.Span);
            try
            {
                await using var scope = _services.CreateAsyncScope();
                await _dispatcher.DispatchAsync(type, content, scope.ServiceProvider, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler failed for {Type}; requeueing.", type);
                try { await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken); }
                catch (Exception nackEx) { _logger.LogError(nackEx, "Nack failed for {Type}.", type); }
            }
        };

        await channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Consuming integration events from '{Queue}' ({Count} routing keys).", _options.QueueName, _registry.Names.Count);

        // Hold the connection open until the app stops (or the connection drops → outer loop reconnects).
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
