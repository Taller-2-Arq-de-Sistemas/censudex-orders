namespace CensudexOrders.Events.Integration.Published;

/// <summary>
/// Integration event published to RabbitMQ when an order is created and needs stock validation
/// This event is consumed by the Inventory service to verify product availability
/// </summary>
public record OrderIssuedForStockValidationIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string EventType => nameof(OrderIssuedForStockValidationIntegrationEvent);

    /// <summary>
    /// Order ID that needs stock validation
    /// </summary>
    public Guid OrderId { get; init; }

    /// <summary>
    /// Order number for reference
    /// </summary>
    public int OrderNumber { get; init; }

    /// <summary>
    /// Customer who placed the order
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// List of products and quantities to validate
    /// </summary>
    public List<StockValidationItem> Items { get; init; } = new();

    public OrderIssuedForStockValidationIntegrationEvent(
        Guid eventId,
        DateTime occurredAt,
        Guid orderId,
        int orderNumber,
        Guid customerId,
        List<StockValidationItem> items)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        Items = items;
    }
}

/// <summary>
/// Represents a product and quantity that needs stock validation
/// </summary>
public record StockValidationItem
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }

    public StockValidationItem(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}
