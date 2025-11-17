using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Data;
using CensudexOrders.Models;
using Microsoft.EntityFrameworkCore;

namespace CensudexOrders.Repositories;

public class OrderProductsRepository(OrdersContext context) : IOrderProductsRepository
{
    private readonly OrdersContext _context = context;

    public void AddProductsToOrder(List<OrderProducts> orderProducts, CancellationToken cancellationToken) =>
        _context.OrderProducts.AddRange(orderProducts);

    public async Task<List<OrderProducts>> GetByOrderId(Guid orderId, CancellationToken cancellationToken) =>
        await _context.OrderProducts
            .Where(op => op.OrderId == orderId)
            .ToListAsync(cancellationToken);
}