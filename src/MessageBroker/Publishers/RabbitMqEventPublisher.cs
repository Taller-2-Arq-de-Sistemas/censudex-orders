using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using CensudexOrders.Events;
using CensudexOrders.MessageBroker.Configuration;
using CensudexOrders.MessageBroker.Interfaces;

namespace CensudexOrders.MessageBroker.Publishers;

/// <summary>
/// Publishes integration events to RabbitMQ
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IRabbitMqConnection _connection;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(
        IRabbitMqConnection connection,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _logger = logger;

        EnsureExchangeExists();
    }

    public Task PublishAsync(IIntegrationEvent @event, string? routingKey = null)
    {
        return Task.Run(() => Publish(@event, routingKey));
    }

    public void Publish(IIntegrationEvent @event, string? routingKey = null)
    {
        if (!_connection.IsConnected)
        {
            _logger.LogWarning("Cannot publish event. RabbitMQ connection is not available");
            _connection.TryConnect();
        }

        if (!_connection.IsConnected)
        {
            throw new InvalidOperationException("RabbitMQ connection is not available");
        }

        var eventType = @event.GetType().Name;
        var actualRoutingKey = routingKey ?? $"censudex.orders.{eventType.ToLowerInvariant()}";

        using var channel = _connection.CreateChannel();

        var message = JsonSerializer.Serialize(@event, @event.GetType(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true; // Make messages durable
        properties.ContentType = "application/json";
        properties.Type = eventType;
        properties.MessageId = @event.EventId.ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        try
        {
            channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: actualRoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published event {EventType} with ID {EventId} to exchange {Exchange} with routing key {RoutingKey}",
                eventType,
                @event.EventId,
                _settings.ExchangeName,
                actualRoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventType} with ID {EventId}",
                eventType,
                @event.EventId);

            throw;
        }
    }

    private void EnsureExchangeExists()
    {
        if (!_connection.TryConnect())
        {
            _logger.LogWarning("Cannot ensure exchange exists. RabbitMQ connection is not available");
            return;
        }

        try
        {
            using var channel = _connection.CreateChannel();

            channel.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: _settings.ExchangeType,
                durable: true,
                autoDelete: false);

            _logger.LogInformation(
                "Ensured exchange {Exchange} of type {Type} exists",
                _settings.ExchangeName,
                _settings.ExchangeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare exchange {Exchange}", _settings.ExchangeName);
        }
    }
}
