using CensudexOrders.Models;

namespace CensudexOrders.Repositories.Interfaces;

public interface IOrdersRepository
{
    void Create(Order order, CancellationToken cancellationToken);
    Task<Order?> Get(int orderNumber, CancellationToken cancellationToken);
    Task<Order?> Get(Guid orderId, CancellationToken cancellationToken);
    void Update(Order order, CancellationToken cancellationToken);
    Task<List<Order>> GetByUserId(Guid userId, CancellationToken cancellationToken);
}