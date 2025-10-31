using Microsoft.EntityFrameworkCore;
using CensudexOrders.Models;

namespace CensudexOrders.Data;

public class OrdersContext : DbContext
{
    public DbSet<Order> Orders { get; set; } = default!;
    public OrdersContext(DbContextOptions<OrdersContext> options) : base(options)
    {
    }
}