using CensudexOrders.Models;

namespace CensudexOrders.Repositories.Interfaces;

public interface IUsersRepository
{
    void Create(User user, CancellationToken cancellationToken);
    Task<User?> Get(Guid userId, CancellationToken cancellationToken);
    void Update(User user, CancellationToken cancellationToken);
    void Delete(User user, CancellationToken cancellationToken);
    Task<bool> Exists(Guid userId, CancellationToken cancellationToken);
}