using CensudexOrders.Models;

namespace CensudexOrders.Repositories.Interfaces;

public interface IProductsRepository
{
    void Create(Product product, CancellationToken cancellationToken);
    Task<Product?> GetById(Guid productId, CancellationToken cancellationToken);
    void Update(Product product, CancellationToken cancellationToken);
    void Delete(Product product, CancellationToken cancellationToken);
}