using CensudexOrders.Data;
using CensudexOrders.Repositories.Interfaces;
namespace CensudexOrders.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly OrdersContext _context;

    public IOrdersRepository OrdersRepository { get; }
    public IProductsRepository ProductsRepository { get; }
    public IUsersRepository UsersRepository { get; }
    public IOrderProductsRepository OrderProductsRepository { get; }

    public UnitOfWork(OrdersContext context,
                      IOrdersRepository ordersRepository,
                      IProductsRepository productsRepository,
                      IUsersRepository usersRepository,
                      IOrderProductsRepository orderProductsRepository)
    {
        _context = context;
        OrdersRepository = ordersRepository;
        ProductsRepository = productsRepository;
        UsersRepository = usersRepository;
        OrderProductsRepository = orderProductsRepository;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}