namespace CensudexOrders.Repositories.Interfaces;

public interface IUnitOfWork
{
    IOrdersRepository OrdersRepository { get; }
    IProductsRepository ProductsRepository { get; }
    IUsersRepository UsersRepository { get; }
    IOrderProductsRepository OrderProductsRepository { get; }
    IOutboxRepository OutboxRepository { get; }
    IProcessedEventsRepository ProcessedEventsRepository { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken);
}