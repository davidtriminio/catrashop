namespace CommerceHub.Infrastructure.Messaging;

/// <summary>Connection and topology settings for RabbitMQ. Bound from the "RabbitMq" configuration section.</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>Topic exchange all integration events are published to (routing key = event type name).</summary>
    public string Exchange { get; set; } = "commercehub.events";

    /// <summary>Durable queue this monolith's consumers read from.</summary>
    public string QueueName { get; set; } = "commercehub.consumers";

    /// <summary>How often the outbox publisher drains unprocessed rows.</summary>
    public int PublishIntervalSeconds { get; set; } = 5;

    /// <summary>Max rows drained per module per publish cycle.</summary>
    public int PublishBatchSize { get; set; } = 100;
}
