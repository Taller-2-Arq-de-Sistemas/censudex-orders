using Microsoft.EntityFrameworkCore;
using CensudexOrders.Data;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.Repositories;

/// <summary>
/// Repository implementation for managing outbox messages
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly OrdersContext _context;

    public OutboxRepository(OrdersContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetUnpublishedAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.PublishedAt = DateTime.UtcNow;
            message.ProcessedCount++;
            message.Error = null;
        }
    }

    public async Task UpdateErrorAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.ProcessedCount++;
            message.Error = error;
        }
    }

    public async Task DeletePublishedOlderThanAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages
            .Where(m => m.PublishedAt != null && m.PublishedAt < date)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
