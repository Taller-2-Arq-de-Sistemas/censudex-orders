using CensudexOrders.Models;

namespace CensudexOrders.Repositories.Interfaces;

public interface IOrderProductsRepository
{
    void AddProductsToOrder(List<OrderProducts> orderProducts, CancellationToken cancellationToken);
    Task<List<OrderProducts>> GetByOrderId(Guid orderId, CancellationToken cancellationToken);
}