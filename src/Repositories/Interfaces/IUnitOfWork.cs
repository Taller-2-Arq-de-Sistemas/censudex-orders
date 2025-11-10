namespace CensudexOrders.Repositories.Interfaces;

public interface IUnitOfWork
{
    IOrdersRepository OrdersRepository { get; }
    IProductsRepository ProductsRepository { get; }
    IUsersRepository UsersRepository { get; }
    IOrderProductsRepository OrderProductsRepository { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken);
}