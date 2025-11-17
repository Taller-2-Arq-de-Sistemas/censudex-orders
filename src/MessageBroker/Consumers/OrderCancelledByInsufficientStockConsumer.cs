using CensudexOrders.Events.Integration.Consumed;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.MessageBroker.Consumers;

/// <summary>
/// Consumes OrderCancelledByInsufficientStock events from RabbitMQ and cancels orders with stock restoration
/// </summary>
public class OrderCancelledByInsufficientStockConsumer : IEventConsumer<OrderCancelledByInsufficientStockIntegrationEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderCancelledByInsufficientStockConsumer> _logger;

    public OrderCancelledByInsufficientStockConsumer(
        IUnitOfWork unitOfWork,
        ILogger<OrderCancelledByInsufficientStockConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCancelledByInsufficientStockIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing OrderCancelledByInsufficientStock event {EventId} for Order {OrderId}",
            @event.EventId,
            @event.OrderId);

        // Check idempotency - skip if already processed
        var eventId = @event.EventId.ToString();
        var alreadyProcessed = await _unitOfWork.ProcessedEventsRepository.ExistsAsync(eventId, @event.EventType, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Event {EventId} already processed. Skipping.",
                @event.EventId);
            return;
        }

        // Get the order
        var order = await _unitOfWork.OrdersRepository.Get(@event.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning(
                "Order {OrderId} not found. Cannot cancel.",
                @event.OrderId);

            // Still mark event as processed to maintain idempotency
            var processedEvent = new ProcessedEvent
            {
                Id = eventId,
                EventType = @event.EventType,
                ProcessedAt = DateTime.UtcNow,
                SourceService = "inventory-service"
            };
            await _unitOfWork.ProcessedEventsRepository.AddAsync(processedEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        // Check if order is already cancelled
        if (order.Status == "cancelado")
        {
            _logger.LogInformation(
                "Order {OrderId} is already cancelled. Skipping cancellation.",
                @event.OrderId);

            // Mark event as processed
            var processedEvent = new ProcessedEvent
            {
                Id = eventId,
                EventType = @event.EventType,
                ProcessedAt = DateTime.UtcNow,
                SourceService = "inventory-service"
            };
            await _unitOfWork.ProcessedEventsRepository.AddAsync(processedEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        // Get order products to restore stock
        var orderProducts = await _unitOfWork.OrderProductsRepository.GetByOrderId(@event.OrderId, cancellationToken);

        // Restore stock for each product
        foreach (var orderProduct in orderProducts)
        {
            var product = await _unitOfWork.ProductsRepository.GetById(orderProduct.ProductId, cancellationToken);
            if (product != null)
            {
                product.Stock += orderProduct.Quantity;
                _unitOfWork.ProductsRepository.Update(product, cancellationToken);

                _logger.LogInformation(
                    "Restored {Quantity} units of stock for Product {ProductId}",
                    orderProduct.Quantity,
                    orderProduct.ProductId);
            }
            else
            {
                _logger.LogWarning(
                    "Product {ProductId} not found. Cannot restore stock for this product.",
                    orderProduct.ProductId);
            }
        }

        // Cancel the order
        order.Status = "cancelado";
        _unitOfWork.OrdersRepository.Update(order, cancellationToken);

        // Mark event as processed
        var processed = new ProcessedEvent
        {
            Id = eventId,
            EventType = @event.EventType,
            ProcessedAt = DateTime.UtcNow,
            SourceService = "inventory-service"
        };
        await _unitOfWork.ProcessedEventsRepository.AddAsync(processed, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully cancelled Order {OrderId} (#{OrderNumber}) due to insufficient stock. Reason: {Reason}",
            @event.OrderId,
            @event.OrderNumber,
            @event.Reason);
    }
}
