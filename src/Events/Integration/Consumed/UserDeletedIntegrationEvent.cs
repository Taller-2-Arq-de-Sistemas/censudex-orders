namespace CensudexOrders.Events.Integration.Consumed;

/// <summary>
/// Integration event consumed from RabbitMQ when a user is deleted in the Users service
/// </summary>
public record UserDeletedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string EventType => nameof(UserDeletedIntegrationEvent);

    /// <summary>
    /// User ID that was deleted
    /// </summary>
    public Guid UserId { get; init; }

    public UserDeletedIntegrationEvent(
        Guid eventId,
        DateTime occurredAt,
        Guid userId)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
        UserId = userId;
    }
}
