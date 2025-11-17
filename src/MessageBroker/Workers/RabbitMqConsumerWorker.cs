using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CensudexOrders.MessageBroker.Configuration;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.MessageBroker.Consumers;
using CensudexOrders.Events.Integration.Consumed;

namespace CensudexOrders.MessageBroker.Workers;

/// <summary>
/// Background worker that consumes events from RabbitMQ and dispatches them to appropriate handlers
/// </summary>
public class RabbitMqConsumerWorker : BackgroundService
{
    private readonly IRabbitMqConnection _connection;
    private readonly RabbitMqSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqConsumerWorker> _logger;
    private IModel? _channel;

    public RabbitMqConsumerWorker(
        IRabbitMqConnection connection,
        IOptions<RabbitMqSettings> settings,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqConsumerWorker> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Worker started");

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Wait for RabbitMQ to be ready

        if (!_connection.TryConnect())
        {
            _logger.LogError("Failed to connect to RabbitMQ. Consumer worker will not start.");
            return;
        }

        try
        {
            _channel = _connection.CreateChannel();

            // Set prefetch count to limit concurrent message processing
            _channel.BasicQos(prefetchSize: 0, prefetchCount: _settings.PrefetchCount, global: false);

            // Declare Dead Letter Exchange (DLX)
            _channel.ExchangeDeclare(
                exchange: _settings.DeadLetterExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Declare Dead Letter Queue (DLQ)
            _channel.QueueDeclare(
                queue: _settings.DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind DLQ to DLX
            _channel.QueueBind(
                queue: _settings.DeadLetterQueueName,
                exchange: _settings.DeadLetterExchangeName,
                routingKey: _settings.QueueName);

            // Declare main queue with DLX configuration
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _settings.DeadLetterExchangeName },
                { "x-dead-letter-routing-key", _settings.QueueName }
            };

            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            // Bind to events from Users service only
            // Note: We do NOT subscribe to our own events (censudex.orders.*)
            _channel.QueueBind(
                queue: _settings.QueueName,
                exchange: _settings.ExchangeName,
                routingKey: "censudex.users.*");

            // Bind to events from Products service
            _channel.QueueBind(
                queue: _settings.QueueName,
                exchange: _settings.ExchangeName,
                routingKey: "censudex.products.*");

            _logger.LogInformation(
                "Queue '{QueueName}' bound to exchange '{ExchangeName}' with routing keys: censudex.users.*, censudex.products.*",
                _settings.QueueName,
                _settings.ExchangeName);

            // Set up consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (sender, eventArgs) =>
            {
                await OnMessageReceivedAsync(eventArgs, stoppingToken);
            };

            _channel.BasicConsume(
                queue: _settings.QueueName,
                autoAck: false, // Manual acknowledgment
                consumer: consumer);

            _logger.LogInformation(
                "Started consuming messages from queue {QueueName}. DLQ: {DLQ}, Max Retries: {MaxRetries}",
                _settings.QueueName,
                _settings.DeadLetterQueueName,
                _settings.MaxRetryCount);

            // Keep the worker running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ Consumer Worker is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RabbitMQ Consumer Worker");
        }
    }

    private async Task OnMessageReceivedAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var messageBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        var eventType = eventArgs.BasicProperties.Type;

        // Get retry count from message headers
        var retryCount = GetRetryCount(eventArgs.BasicProperties);

        _logger.LogInformation(
            "Received message of type {EventType} with routing key {RoutingKey} (Retry: {RetryCount}/{MaxRetry})",
            eventType,
            eventArgs.RoutingKey,
            retryCount,
            _settings.MaxRetryCount);

        try
        {
            // Route to appropriate consumer based on event type
            var handled = await RouteToConsumerAsync(eventType, messageBody, cancellationToken);

            if (handled)
            {
                // Acknowledge the message
                _channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                _logger.LogInformation("Successfully processed and acknowledged message of type {EventType}", eventType);
            }
            else
            {
                // Unknown event type - acknowledge anyway to avoid blocking the queue
                _channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                _logger.LogWarning("Unknown event type {EventType}. Message acknowledged but not processed.", eventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message of type {EventType}", eventType);

            // Check if max retries exceeded
            if (retryCount >= _settings.MaxRetryCount)
            {
                _logger.LogError(
                    "Max retry count ({MaxRetryCount}) exceeded for message of type {EventType}. Sending to Dead Letter Queue.",
                    _settings.MaxRetryCount,
                    eventType);

                // Reject without requeue - message will go to DLQ
                _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
            else
            {
                // Increment retry count and requeue with delay
                _logger.LogWarning(
                    "Retry attempt {RetryCount}/{MaxRetryCount} for message of type {EventType}",
                    retryCount + 1,
                    _settings.MaxRetryCount,
                    eventType);

                // Reject and requeue for retry
                _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);

                // Republish with incremented retry count and exponential backoff delay
                await RepublishWithRetryAsync(eventArgs, retryCount + 1, cancellationToken);
            }
        }
    }

    private int GetRetryCount(IBasicProperties properties)
    {
        if (properties.Headers != null && properties.Headers.TryGetValue("x-retry-count", out var value))
        {
            return value is int intValue ? intValue : Convert.ToInt32(value);
        }
        return 0;
    }

    private async Task RepublishWithRetryAsync(BasicDeliverEventArgs eventArgs, int retryCount, CancellationToken cancellationToken)
    {
        try
        {
            // Calculate exponential backoff delay (in milliseconds)
            var delayMs = (int)Math.Pow(2, retryCount) * 1000; // 2s, 4s, 8s...
            await Task.Delay(delayMs, cancellationToken);

            using var channel = _connection.CreateChannel();
            var properties = channel.CreateBasicProperties();

            // Copy original properties
            properties.Persistent = eventArgs.BasicProperties.Persistent;
            properties.ContentType = eventArgs.BasicProperties.ContentType;
            properties.Type = eventArgs.BasicProperties.Type;
            properties.MessageId = eventArgs.BasicProperties.MessageId;
            properties.Timestamp = eventArgs.BasicProperties.Timestamp;

            // Add/update retry count header
            properties.Headers = eventArgs.BasicProperties.Headers != null
                ? new Dictionary<string, object>(eventArgs.BasicProperties.Headers)
                : new Dictionary<string, object>();
            properties.Headers["x-retry-count"] = retryCount;

            // Republish to the same queue
            channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: eventArgs.RoutingKey,
                basicProperties: properties,
                body: eventArgs.Body);

            _logger.LogInformation(
                "Republished message of type {EventType} with retry count {RetryCount} after {DelayMs}ms delay",
                eventArgs.BasicProperties.Type,
                retryCount,
                delayMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to republish message for retry");
        }
    }

    private async Task<bool> RouteToConsumerAsync(string eventType, string messageBody, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        // Route to appropriate consumer based on event type
        var result = eventType switch
        {
            nameof(UserCreatedIntegrationEvent) =>
                await HandleEventAsync<UserCreatedIntegrationEvent, UserCreatedConsumer>(
                    messageBody, scope, cancellationToken),

            nameof(UserUpdatedIntegrationEvent) =>
                await HandleEventAsync<UserUpdatedIntegrationEvent, UserUpdatedConsumer>(
                    messageBody, scope, cancellationToken),

            nameof(UserDeletedIntegrationEvent) =>
                await HandleEventAsync<UserDeletedIntegrationEvent, UserDeletedConsumer>(
                    messageBody, scope, cancellationToken),

            nameof(ProductCreatedIntegrationEvent) =>
                await HandleEventAsync<ProductCreatedIntegrationEvent, ProductCreatedConsumer>(
                    messageBody, scope, cancellationToken),

            nameof(ProductUpdatedIntegrationEvent) =>
                await HandleEventAsync<ProductUpdatedIntegrationEvent, ProductUpdatedConsumer>(
                    messageBody, scope, cancellationToken),

            nameof(ProductDeletedIntegrationEvent) =>
                await HandleEventAsync<ProductDeletedIntegrationEvent, ProductDeletedConsumer>(
                    messageBody, scope, cancellationToken),

            nameof(OrderCancelledByInsufficientStockIntegrationEvent) =>
                await HandleEventAsync<OrderCancelledByInsufficientStockIntegrationEvent, OrderCancelledByInsufficientStockConsumer>(
                    messageBody, scope, cancellationToken),

            _ => false // Unknown event type
        };

        return result;
    }

    private async Task<bool> HandleEventAsync<TEvent, TConsumer>(
        string messageBody,
        IServiceScope scope,
        CancellationToken cancellationToken)
        where TConsumer : IEventConsumer<TEvent>
    {
        try
        {
            var @event = JsonSerializer.Deserialize<TEvent>(messageBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (@event == null)
            {
                _logger.LogWarning("Failed to deserialize event of type {EventType}", typeof(TEvent).Name);
                return false;
            }

            var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
            await consumer.HandleAsync(@event, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event of type {EventType}", typeof(TEvent).Name);
            throw;
        }
    }

    public override void Dispose()
    {
        try
        {
            if (_channel != null)
            {
                if (_channel.IsOpen)
                {
                    _channel.Close();
                }
                _channel.Dispose();
                _channel = null;
            }
        }
        catch (ObjectDisposedException)
        {
            // Channel already disposed, ignore
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ channel");
        }
        finally
        {
            base.Dispose();
        }
    }
}
