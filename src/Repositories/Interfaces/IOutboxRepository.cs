using CensudexOrders.Models;

namespace CensudexOrders.Repositories.Interfaces;

/// <summary>
/// Repository for managing outbox messages
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a new outbox message to the database
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unpublished outbox messages ordered by creation time
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<OutboxMessage>> GetUnpublishedAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as published
    /// </summary>
    Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a message with error information after a failed publish attempt
    /// </summary>
    Task UpdateErrorAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes published messages older than the specified date (for cleanup)
    /// </summary>
    Task DeletePublishedOlderThanAsync(DateTime date, CancellationToken cancellationToken = default);
}
