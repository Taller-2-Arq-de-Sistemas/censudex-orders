using CensudexOrders.Models;

namespace CensudexOrders.Repositories.Interfaces;

/// <summary>
/// Repository for managing processed events (idempotency tracking)
/// </summary>
public interface IProcessedEventsRepository
{
    /// <summary>
    /// Checks if an event has already been processed
    /// </summary>
    /// <param name="eventId">Event ID from the integration event</param>
    /// <param name="eventType">Type of the event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if event was already processed, false otherwise</returns>
    Task<bool> ExistsAsync(string eventId, string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that an event has been processed
    /// </summary>
    /// <param name="processedEvent">The processed event to record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes processed events older than the specified date (for cleanup)
    /// </summary>
    /// <param name="date">Delete events processed before this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteOlderThanAsync(DateTime date, CancellationToken cancellationToken = default);
}
