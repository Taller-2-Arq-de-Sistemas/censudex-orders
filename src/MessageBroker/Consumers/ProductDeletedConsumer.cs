using CensudexOrders.Events.Integration.Consumed;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.MessageBroker.Consumers;

/// <summary>
/// Consumes ProductDeleted events from RabbitMQ and soft-deletes products in the local database
/// </summary>
public class ProductDeletedConsumer : IEventConsumer<ProductDeletedIntegrationEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductDeletedConsumer> _logger;

    public ProductDeletedConsumer(
        IUnitOfWork unitOfWork,
        ILogger<ProductDeletedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(ProductDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing ProductDeleted event {EventId} for Product {ProductId}",
            @event.EventId,
            @event.ProductId);

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

        // Get existing product
        var product = await _unitOfWork.ProductsRepository.GetById(@event.ProductId, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning(
                "Product {ProductId} not found. Cannot soft-delete.",
                @event.ProductId);

            // Still mark event as processed to maintain idempotency
            var processedEvent = new ProcessedEvent
            {
                Id = eventId,
                EventType = @event.EventType,
                ProcessedAt = DateTime.UtcNow,
                SourceService = "products-service"
            };
            await _unitOfWork.ProcessedEventsRepository.AddAsync(processedEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        // Soft-delete product by setting IsActive to false
        product.IsActive = false;
        _unitOfWork.ProductsRepository.Update(product, cancellationToken);

        // Mark event as processed
        var processed = new ProcessedEvent
        {
            Id = eventId,
            EventType = @event.EventType,
            ProcessedAt = DateTime.UtcNow,
            SourceService = "products-service"
        };
        await _unitOfWork.ProcessedEventsRepository.AddAsync(processed, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully soft-deleted Product {ProductId} from event {EventId}",
            @event.ProductId,
            @event.EventId);
    }
}
