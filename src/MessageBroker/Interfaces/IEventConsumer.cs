namespace CensudexOrders.MessageBroker.Interfaces;

/// <summary>
/// Interface for event consumers that process integration events from RabbitMQ
/// </summary>
/// <typeparam name="TEvent">Type of integration event to consume</typeparam>
public interface IEventConsumer<in TEvent>
{
    /// <summary>
    /// Processes an integration event received from RabbitMQ
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
