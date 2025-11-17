using MediatR;

namespace CensudexOrders.Events;

/// <summary>
/// Marker interface for domain events that occur within the domain model.
/// Domain events are handled within the same transaction boundary.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    DateTime OccurredAt { get; }
}
