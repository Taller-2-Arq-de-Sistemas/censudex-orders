namespace CensudexOrders.Models;

/// <summary>
/// Tracks integration events that have been processed to ensure idempotent message handling.
/// Prevents duplicate processing when the same event is received multiple times from RabbitMQ.
/// </summary>
public class ProcessedEvent
{
    /// <summary>
    /// Event ID from the integration event (unique identifier)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of the event (e.g., "UserCreated", "ProductUpdated")
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when we processed this event
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source service that published this event (e.g., "users-service", "products-service")
    /// </summary>
    public string SourceService { get; set; } = string.Empty;
}
