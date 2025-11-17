namespace CensudexOrders.MessageBroker.Configuration;

/// <summary>
/// Configuration settings for RabbitMQ connection
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// RabbitMQ host name or IP address
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port (default: 5672)
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host for isolation
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Exchange name for publishing events
    /// </summary>
    public string ExchangeName { get; set; } = "censudex.events";

    /// <summary>
    /// Exchange type (topic, direct, fanout, headers)
    /// </summary>
    public string ExchangeType { get; set; } = "topic";

    /// <summary>
    /// Queue name for consuming events (specific to this service)
    /// </summary>
    public string QueueName { get; set; } = "censudex.orders.queue";

    /// <summary>
    /// Routing key pattern for subscriptions (deprecated - routing keys are now hardcoded in RabbitMqConsumerWorker)
    /// The service subscribes to: censudex.users.* and censudex.products.*
    /// It does NOT subscribe to its own events (censudex.orders.*)
    /// </summary>
    [Obsolete("This setting is no longer used. Routing keys are hardcoded in RabbitMqConsumerWorker.")]
    public string RoutingKeyPattern { get; set; } = "censudex.orders.#";

    /// <summary>
    /// Number of retry attempts for connection failures
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// Delay between retry attempts in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 3;

    /// <summary>
    /// Enable automatic recovery on connection failure
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Network recovery interval in seconds
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Dead Letter Queue name for failed messages
    /// </summary>
    public string DeadLetterQueueName { get; set; } = "censudex.orders.dlq";

    /// <summary>
    /// Dead Letter Exchange name
    /// </summary>
    public string DeadLetterExchangeName { get; set; } = "censudex.dlx";

    /// <summary>
    /// Maximum number of retries before sending to DLQ
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Prefetch count for consumer (number of messages to process concurrently)
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;
}
