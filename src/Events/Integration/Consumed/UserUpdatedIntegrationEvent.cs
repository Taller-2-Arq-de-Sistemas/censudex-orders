namespace CensudexOrders.Events.Integration.Consumed;

/// <summary>
/// Integration event consumed from RabbitMQ when a user is updated in the Users service
/// </summary>
public record UserUpdatedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string EventType => nameof(UserUpdatedIntegrationEvent);

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// User's first name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// User's last names
    /// </summary>
    public string LastNames { get; init; } = string.Empty;

    /// <summary>
    /// User's address
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; init; } = string.Empty;

    public UserUpdatedIntegrationEvent(
        Guid eventId,
        DateTime occurredAt,
        Guid userId,
        string name,
        string lastNames,
        string address,
        string email)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
        UserId = userId;
        Name = name;
        LastNames = lastNames;
        Address = address;
        Email = email;
    }
}
