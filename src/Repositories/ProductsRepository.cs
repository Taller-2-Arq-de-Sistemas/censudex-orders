using CensudexOrders.Data;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.Repositories;

public class ProductsRepository(OrdersContext context) : IProductsRepository
{
    private readonly OrdersContext _context = context;
    public void Create(Product product, CancellationToken cancellationToken) =>
        _context.Products.Add(product);

    public async Task<Product?> GetById(Guid productId, CancellationToken cancellationToken) =>
        await _context.Products.FindAsync(productId, cancellationToken);

    public void Update(Product product, CancellationToken cancellationToken) =>
        _context.Products.Update(product);

    public void Delete(Product product, CancellationToken cancellationToken) =>
        _context.Products.Remove(product);
}