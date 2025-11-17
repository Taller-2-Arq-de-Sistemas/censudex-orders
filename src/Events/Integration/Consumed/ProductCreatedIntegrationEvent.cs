namespace CensudexOrders.Events.Integration.Consumed;

/// <summary>
/// Integration event consumed from RabbitMQ when a product is created in the Products service
/// </summary>
public record ProductCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string EventType => nameof(ProductCreatedIntegrationEvent);

    /// <summary>
    /// Product ID
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Product name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Product price
    /// </summary>
    public int Price { get; init; }

    /// <summary>
    /// Product stock quantity
    /// </summary>
    public int Stock { get; init; }

    public ProductCreatedIntegrationEvent(
        Guid eventId,
        DateTime occurredAt,
        Guid productId,
        string name,
        int price,
        int stock)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
        ProductId = productId;
        Name = name;
        Price = price;
        Stock = stock;
    }
}
