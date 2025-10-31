namespace CensudexOrders.Models;

public class Order
{
    public Guid Id { get; set; }
    public int OrderNumber { get; set; }
    public string Status { get; set; } = default!;
    public int TotalCharge { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CustomerId { get; set; }
}
