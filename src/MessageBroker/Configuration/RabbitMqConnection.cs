using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using CensudexOrders.MessageBroker.Interfaces;

namespace CensudexOrders.MessageBroker.Configuration;

/// <summary>
/// Manages persistent RabbitMQ connections with automatic recovery
/// </summary>
public class RabbitMqConnection : IRabbitMqConnection
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConnection> _logger;
    private IConnection? _connection;
    private bool _disposed;
    private readonly object _lock = new();

    public RabbitMqConnection(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqConnection> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public bool TryConnect()
    {
        lock (_lock)
        {
            if (IsConnected)
            {
                return true;
            }

            _logger.LogInformation(
                "Attempting to connect to RabbitMQ at {Host}:{Port}",
                _settings.Host,
                _settings.Port);

            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                VirtualHost = _settings.VirtualHost,
                UserName = _settings.Username,
                Password = _settings.Password,
                AutomaticRecoveryEnabled = _settings.AutomaticRecoveryEnabled,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_settings.NetworkRecoveryIntervalSeconds),
                DispatchConsumersAsync = true // Enable async consumers
            };

            var retryCount = 0;
            while (retryCount < _settings.RetryCount)
            {
                try
                {
                    _connection = factory.CreateConnection();

                    if (IsConnected)
                    {
                        _connection.ConnectionShutdown += OnConnectionShutdown;
                        _connection.CallbackException += OnCallbackException;
                        _connection.ConnectionBlocked += OnConnectionBlocked;

                        _logger.LogInformation(
                            "Successfully connected to RabbitMQ at {Host}:{Port}",
                            _settings.Host,
                            _settings.Port);

                        return true;
                    }
                }
                catch (BrokerUnreachableException ex)
                {
                    retryCount++;
                    _logger.LogWarning(
                        ex,
                        "Failed to connect to RabbitMQ. Retry {RetryCount}/{MaxRetries}",
                        retryCount,
                        _settings.RetryCount);

                    if (retryCount < _settings.RetryCount)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(_settings.RetryDelaySeconds));
                    }
                }
            }

            _logger.LogError(
                "Could not connect to RabbitMQ after {RetryCount} attempts",
                _settings.RetryCount);

            return false;
        }
    }

    public IModel CreateChannel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connection available to create a channel");
        }

        return _connection!.CreateModel();
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);

        TryConnect();
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning(e.Exception, "RabbitMQ callback exception");

        TryConnect();
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("RabbitMQ connection shutdown. Reason: {Reason}", e.ReplyText);

        TryConnect();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            if (_connection != null)
            {
                _connection.ConnectionShutdown -= OnConnectionShutdown;
                _connection.CallbackException -= OnCallbackException;
                _connection.ConnectionBlocked -= OnConnectionBlocked;

                _connection.Close();
                _connection.Dispose();
            }

            _logger.LogInformation("RabbitMQ connection disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}
