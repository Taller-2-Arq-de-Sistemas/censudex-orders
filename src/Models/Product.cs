namespace CensudexOrders.Models;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public int Price { get; set; }
}
