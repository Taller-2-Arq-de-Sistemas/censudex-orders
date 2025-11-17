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
    public IOutboxRepository OutboxRepository { get; }
    public IProcessedEventsRepository ProcessedEventsRepository { get; }

    public UnitOfWork(OrdersContext context,
                      IOrdersRepository ordersRepository,
                      IProductsRepository productsRepository,
                      IUsersRepository usersRepository,
                      IOrderProductsRepository orderProductsRepository,
                      IOutboxRepository outboxRepository,
                      IProcessedEventsRepository processedEventsRepository)
    {
        _context = context;
        OrdersRepository = ordersRepository;
        ProductsRepository = productsRepository;
        UsersRepository = usersRepository;
        OrderProductsRepository = orderProductsRepository;
        OutboxRepository = outboxRepository;
        ProcessedEventsRepository = processedEventsRepository;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}