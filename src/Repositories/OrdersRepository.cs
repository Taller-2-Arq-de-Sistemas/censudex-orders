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
        _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public void UpdateStatus(Guid orderId, string status, CancellationToken cancellationToken) =>
        _context.Orders.Where(o => o.Id == orderId).ExecuteUpdate(o => o.SetProperty(p => p.Status, status));

    public void Cancel(Guid orderId, CancellationToken cancellationToken) =>
        _context.Orders.Where(o => o.Id == orderId).ExecuteUpdate(o => o.SetProperty(p => p.Status, "cancelado"));

    public async Task<List<Order>> GetByUserId(string userId, CancellationToken cancellationToken) =>
        await _context.Orders
            .Where(o => o.CustomerId.ToString() == userId)
            .ToListAsync(cancellationToken);
}