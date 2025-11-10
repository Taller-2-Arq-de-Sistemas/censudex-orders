using CensudexOrders.Data;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CensudexOrders.Repositories;

public class UsersRepository(OrdersContext context) : IUsersRepository
{
    private readonly OrdersContext _context = context;
    public void Create(User user, CancellationToken cancellationToken) =>
        _context.Users.Add(user);

    public async Task<User?> Get(Guid userId, CancellationToken cancellationToken) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public void Update(User user, CancellationToken cancellationToken) =>
        _context.Users.Update(user);

    public void Delete(User user, CancellationToken cancellationToken) =>
        _context.Users.Remove(user);

    public async Task<bool> Exists(Guid userId, CancellationToken cancellationToken) =>
        await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);

}