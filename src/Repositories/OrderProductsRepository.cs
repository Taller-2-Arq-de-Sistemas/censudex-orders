using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Data;
using CensudexOrders.Models;

namespace CensudexOrders.Repositories;

public class OrderProductsRepository(OrdersContext context) : IOrderProductsRepository
{
    private readonly OrdersContext _context = context;

    public void AddProductsToOrder(List<OrderProducts> orderProducts, CancellationToken cancellationToken) =>
        _context.OrderProducts.AddRange(orderProducts);
}