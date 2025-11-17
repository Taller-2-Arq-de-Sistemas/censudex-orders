namespace CensudexOrders.Events.Integration.Consumed;

/// <summary>
/// Integration event consumed from RabbitMQ when a product is deleted in the Products service
/// </summary>
public record ProductDeletedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string EventType => nameof(ProductDeletedIntegrationEvent);

    /// <summary>
    /// Product ID that was deleted
    /// </summary>
    public Guid ProductId { get; init; }

    public ProductDeletedIntegrationEvent(
        Guid eventId,
        DateTime occurredAt,
        Guid productId)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
        ProductId = productId;
    }
}
