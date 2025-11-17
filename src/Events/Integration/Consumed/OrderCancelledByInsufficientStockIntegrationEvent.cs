namespace CensudexOrders.Events.Integration.Consumed;

/// <summary>
/// Integration event consumed from RabbitMQ when an order is cancelled due to insufficient stock
/// This event is published by the Inventory service after stock validation fails
/// </summary>
public record OrderCancelledByInsufficientStockIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string EventType => nameof(OrderCancelledByInsufficientStockIntegrationEvent);

    /// <summary>
    /// Order ID that needs to be cancelled
    /// </summary>
    public Guid OrderId { get; init; }

    /// <summary>
    /// Order number for reference
    /// </summary>
    public int OrderNumber { get; init; }

    /// <summary>
    /// Reason for cancellation (e.g., which products had insufficient stock)
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    public OrderCancelledByInsufficientStockIntegrationEvent(
        Guid eventId,
        DateTime occurredAt,
        Guid orderId,
        int orderNumber,
        string reason)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
        OrderId = orderId;
        OrderNumber = orderNumber;
        Reason = reason;
    }
}
