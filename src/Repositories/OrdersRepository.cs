using Microsoft.EntityFrameworkCore;
using CensudexOrders.Data;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.Repositories;

public class OrdersRepository(OrdersContext context) : IOrdersRepository
{
    private readonly OrdersContext _context = context;
    public void Create(Order order, CancellationToken cancellationToken) =>
        _context.Orders.Add(order);

    public Task<Order?> Get(int orderNumber, CancellationToken cancellationToken) =>
        _context.Orders.Include(o => o.OrderProducts)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public Task<Order?> Get(Guid orderId, CancellationToken cancellationToken) =>
        _context.Orders.Include(o => o.OrderProducts)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public void Update(Order order, CancellationToken cancellationToken) =>
        _context.Orders.Update(order);

    public async Task<List<Order>> GetByUserId(Guid userId, CancellationToken cancellationToken) =>
        await _context.Orders
            .Where(o => o.CustomerId == userId)
            .Include(o => o.OrderProducts)
            .ToListAsync(cancellationToken);
}