using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CommerceHub.Infrastructure.Messaging;

/// <summary>
/// Publishes integration events to a durable topic exchange, routing by event type name. The connection and
/// channel are created lazily and re-created if they drop; a publish failure propagates so the Outbox row is
/// left unprocessed and retried on the next cycle.
/// </summary>
public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(string type, string jsonContent, Guid messageId, Guid? correlationId, CancellationToken ct = default)
    {
        var channel = await EnsureChannelAsync(ct);

        var properties = new BasicProperties
        {
            MessageId = messageId.ToString(),
            CorrelationId = correlationId?.ToString(),
            ContentType = "application/json",
            Persistent = true,
            Type = type,
        };

        await channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: type,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(jsonContent),
            cancellationToken: ct);
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;

        await _gate.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true }) return _channel;

            if (_connection is not { IsOpen: true })
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    VirtualHost = _options.VirtualHost,
                };
                _connection = await factory.CreateConnectionAsync(ct);
                _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", _options.HostName, _options.Port);
            }

            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        _gate.Dispose();
    }
}
