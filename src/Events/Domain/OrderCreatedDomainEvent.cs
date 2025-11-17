namespace CensudexOrders.Events.Domain;

/// <summary>
/// Domain event raised when an order is created
/// </summary>
public record OrderCreatedDomainEvent : DomainEventBase
{
    public Guid OrderId { get; init; }
    public int OrderNumber { get; init; }
    public Guid CustomerId { get; init; }
    public int TotalCharge { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<OrderProductItem> Products { get; init; } = new();

    public OrderCreatedDomainEvent(
        Guid orderId,
        int orderNumber,
        Guid customerId,
        int totalCharge,
        string status,
        List<OrderProductItem> products)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        TotalCharge = totalCharge;
        Status = status;
        Products = products;
    }
}

/// <summary>
/// Represents a product in the order
/// </summary>
public record OrderProductItem
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }

    public OrderProductItem(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}
