using CensudexOrders.Events.Integration.Consumed;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.MessageBroker.Consumers;

/// <summary>
/// Consumes ProductUpdated events from RabbitMQ and updates products in the local database
/// </summary>
public class ProductUpdatedConsumer : IEventConsumer<ProductUpdatedIntegrationEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductUpdatedConsumer> _logger;

    public ProductUpdatedConsumer(
        IUnitOfWork unitOfWork,
        ILogger<ProductUpdatedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(ProductUpdatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing ProductUpdated event {EventId} for Product {ProductId}",
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
                "Product {ProductId} not found. Cannot update. Creating new product instead.",
                @event.ProductId);

            // Create product if doesn't exist (out-of-order event handling)
            product = new Product
            {
                Id = @event.ProductId,
                Name = @event.Name,
                Price = @event.Price,
                Stock = @event.Stock,
                IsActive = true
            };

            _unitOfWork.ProductsRepository.Create(product, cancellationToken);
        }
        else
        {
            // Update existing product
            product.Name = @event.Name;
            product.Price = @event.Price;
            product.Stock = @event.Stock;

            _unitOfWork.ProductsRepository.Update(product, cancellationToken);
        }

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
            "Successfully updated Product {ProductId} from event {EventId}",
            @event.ProductId,
            @event.EventId);
    }
}
