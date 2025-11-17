using CensudexOrders.Events;

namespace CensudexOrders.Models;

/// <summary>
/// Base class for entities that support domain events
/// </summary>
public abstract class EntityBase
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Domain events that have been raised by this entity
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raise a domain event
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clear all domain events (called after they've been dispatched)
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
