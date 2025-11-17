using System.Text.Json;
using MediatR;
using CensudexOrders.Events.Domain;
using CensudexOrders.Events.Integration.Published;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.Events.Handlers;

/// <summary>
/// Handles OrderCreatedDomainEvent by saving OrderIssuedForStockValidation integration event to the outbox
/// </summary>
public class OrderCreatedDomainEventHandler : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<OrderCreatedDomainEventHandler> _logger;

    public OrderCreatedDomainEventHandler(
        IOutboxRepository outboxRepository,
        ILogger<OrderCreatedDomainEventHandler> logger)
    {
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling OrderCreatedDomainEvent for Order {OrderId}",
            notification.OrderId);

        // Map domain event products to integration event items
        var stockValidationItems = notification.Products
            .Select(p => new StockValidationItem(p.ProductId, p.Quantity))
            .ToList();

        // Create integration event for stock validation
        var integrationEvent = new OrderIssuedForStockValidationIntegrationEvent(
            eventId: notification.EventId,
            occurredAt: notification.OccurredAt,
            orderId: notification.OrderId,
            orderNumber: notification.OrderNumber,
            customerId: notification.CustomerId,
            items: stockValidationItems);

        // Serialize the integration event
        var payload = JsonSerializer.Serialize(integrationEvent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Save to outbox
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = integrationEvent.EventType,
            Payload = payload,
            OccurredAt = integrationEvent.OccurredAt,
            CreatedAt = DateTime.UtcNow
        };

        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

        _logger.LogInformation(
            "Saved OrderIssuedForStockValidationIntegrationEvent to outbox with ID {OutboxMessageId} for Order {OrderId}",
            outboxMessage.Id,
            notification.OrderId);
    }
}
