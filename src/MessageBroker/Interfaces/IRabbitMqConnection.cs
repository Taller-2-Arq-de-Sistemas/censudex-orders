using RabbitMQ.Client;

namespace CensudexOrders.MessageBroker.Interfaces;

/// <summary>
/// Interface for managing RabbitMQ connections
/// </summary>
public interface IRabbitMqConnection : IDisposable
{
    /// <summary>
    /// Gets whether the connection is currently open
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Creates a new channel for publishing or consuming messages
    /// </summary>
    IModel CreateChannel();

    /// <summary>
    /// Attempts to connect to RabbitMQ
    /// </summary>
    bool TryConnect();
}
