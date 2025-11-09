using Microsoft.EntityFrameworkCore;
using CensudexOrders.Models;

namespace CensudexOrders.Data;

public class OrdersContext : DbContext
{
    public DbSet<Order> Orders { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<OrderProducts> OrderProducts { get; set; } = default!;
    public DbSet<User> Users { get; set; } = default!;
    public OrdersContext(DbContextOptions<OrdersContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .Property(o => o.OrderNumber)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Order>()
            .HasAlternateKey(o => o.OrderNumber);
    }
}