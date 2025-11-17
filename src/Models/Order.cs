using CensudexOrders.Events.Domain;

namespace CensudexOrders.Models;

public class Order : EntityBase
{
    public Guid Id { get; set; }
    public int OrderNumber { get; set; }
    public string Status { get; set; } = default!;
    public int TotalCharge { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = default!;
    public ICollection<OrderProducts> OrderProducts { get; set; } = [];

    /// <summary>
    /// Raises a domain event when the order is created
    /// </summary>
    public void RaiseOrderCreatedEvent()
    {
        var products = OrderProducts
            .Select(op => new OrderProductItem(op.ProductId, op.Quantity))
            .ToList();

        var domainEvent = new OrderCreatedDomainEvent(
            orderId: Id,
            orderNumber: OrderNumber,
            customerId: CustomerId,
            totalCharge: TotalCharge,
            status: Status,
            products: products);

        RaiseDomainEvent(domainEvent);
    }
}
