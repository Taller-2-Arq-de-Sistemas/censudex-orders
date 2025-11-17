using Microsoft.EntityFrameworkCore;
using CensudexOrders.Data;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.Repositories;

/// <summary>
/// Repository implementation for managing processed events
/// </summary>
public class ProcessedEventsRepository : IProcessedEventsRepository
{
    private readonly OrdersContext _context;

    public ProcessedEventsRepository(OrdersContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string eventId, string eventType, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessedEvents
            .AnyAsync(e => e.Id == eventId && e.EventType == eventType, cancellationToken);
    }

    public async Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default)
    {
        await _context.ProcessedEvents.AddAsync(processedEvent, cancellationToken);
    }

    public async Task DeleteOlderThanAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await _context.ProcessedEvents
            .Where(e => e.ProcessedAt < date)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
