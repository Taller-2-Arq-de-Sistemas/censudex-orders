namespace CensudexOrders.Models;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string LastNames { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
