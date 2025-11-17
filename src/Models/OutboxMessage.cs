namespace CensudexOrders.Models;

/// <summary>
/// Outbox pattern entity for reliable event publishing to RabbitMQ.
/// Events are saved to the database in the same transaction as domain changes,
/// then published asynchronously by a background worker.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier for the outbox message
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of the event (fully qualified type name for deserialization)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized event payload (JSON)
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the event occurred in the domain
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Timestamp when the outbox message was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the message was successfully published to RabbitMQ
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Number of times publishing this message has been attempted
    /// </summary>
    public int ProcessedCount { get; set; } = 0;

    /// <summary>
    /// Error message from last failed publish attempt (if any)
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Whether this message has been successfully published
    /// </summary>
    public bool IsPublished => PublishedAt.HasValue;
}
