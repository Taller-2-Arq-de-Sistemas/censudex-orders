using CensudexOrders.Models;

namespace CensudexOrders.Repositories.Interfaces;

public interface IOrdersRepository
{
    void Create(Order order, CancellationToken cancellationToken);
    Task<Order?> Get(int orderNumber, CancellationToken cancellationToken);
    void UpdateStatus(Guid orderId, string status, CancellationToken cancellationToken);
    void Cancel(Guid orderId, CancellationToken cancellationToken);
    Task<List<Order>> GetByUserId(string userId, CancellationToken cancellationToken);
}