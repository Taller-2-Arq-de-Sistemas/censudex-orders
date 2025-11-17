namespace CensudexOrders.Events;

/// <summary>
/// Marker interface for integration events that are published to external systems (RabbitMQ).
/// Integration events cross service boundaries and are handled asynchronously.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Type name of the event (used for routing and deserialization)
    /// </summary>
    string EventType { get; }
}
