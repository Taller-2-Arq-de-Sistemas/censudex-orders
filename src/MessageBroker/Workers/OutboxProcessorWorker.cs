using CensudexOrders.Data;
using CensudexOrders.Events;
using CensudexOrders.Events.Integration.Published;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CensudexOrders.MessageBroker.Workers;

/// <summary>
/// Background worker that processes outbox messages and publishes them to RabbitMQ
/// </summary>
public class OutboxProcessorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorWorker> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10);
    private readonly int _batchSize = 100;

    public OutboxProcessorWorker(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Processor Worker stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var context = scope.ServiceProvider.GetRequiredService<OrdersContext>();

        // Get unpublished messages
        var messages = await outboxRepository.GetUnpublishedAsync(_batchSize, cancellationToken);

        if (!messages.Any())
        {
            return;
        }

        _logger.LogInformation(
            "Processing {MessageCount} outbox messages",
            messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Deserialize the integration event
                var integrationEvent = DeserializeEvent(message);

                if (integrationEvent == null)
                {
                    _logger.LogWarning(
                        "Failed to deserialize outbox message {MessageId} of type {EventType}",
                        message.Id,
                        message.EventType);

                    await outboxRepository.UpdateErrorAsync(
                        message.Id,
                        "Failed to deserialize event",
                        cancellationToken);

                    continue;
                }

                // Publish to RabbitMQ
                eventPublisher.Publish(integrationEvent);

                // Mark as published
                await outboxRepository.MarkAsPublishedAsync(message.Id, cancellationToken);

                _logger.LogInformation(
                    "Published outbox message {MessageId} of type {EventType}",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish outbox message {MessageId} of type {EventType}",
                    message.Id,
                    message.EventType);

                await outboxRepository.UpdateErrorAsync(
                    message.Id,
                    ex.Message,
                    cancellationToken);
            }
        }

        // Save all changes (published flags and errors)
        await context.SaveChangesAsync(cancellationToken);

        // Cleanup old published messages (older than 7 days)
        var cleanupDate = DateTime.UtcNow.AddDays(-7);
        await outboxRepository.DeletePublishedOlderThanAsync(cleanupDate, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private IIntegrationEvent? DeserializeEvent(Models.OutboxMessage message)
    {
        return message.EventType switch
        {
            nameof(OrderIssuedForStockValidationIntegrationEvent) =>
                System.Text.Json.JsonSerializer.Deserialize<OrderIssuedForStockValidationIntegrationEvent>(message.Payload),
            _ => null
        };
    }
}
