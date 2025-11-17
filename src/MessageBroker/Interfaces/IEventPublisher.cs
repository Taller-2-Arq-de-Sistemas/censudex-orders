using CensudexOrders.Events;

namespace CensudexOrders.MessageBroker.Interfaces;

/// <summary>
/// Interface for publishing integration events to RabbitMQ
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an integration event to RabbitMQ
    /// </summary>
    /// <param name="event">The event to publish</param>
    /// <param name="routingKey">Optional routing key (defaults to event type)</param>
    Task PublishAsync(IIntegrationEvent @event, string? routingKey = null);

    /// <summary>
    /// Publishes an integration event to RabbitMQ synchronously
    /// </summary>
    /// <param name="event">The event to publish</param>
    /// <param name="routingKey">Optional routing key (defaults to event type)</param>
    void Publish(IIntegrationEvent @event, string? routingKey = null);
}
