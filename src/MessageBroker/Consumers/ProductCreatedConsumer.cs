using CensudexOrders.Events.Integration.Consumed;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.MessageBroker.Consumers;

/// <summary>
/// Consumes ProductCreated events from RabbitMQ and creates products in the local database
/// </summary>
public class ProductCreatedConsumer : IEventConsumer<ProductCreatedIntegrationEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(
        IUnitOfWork unitOfWork,
        ILogger<ProductCreatedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(ProductCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing ProductCreated event {EventId} for Product {ProductId}",
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

        // Check if product already exists (could be from manual seeding)
        var existingProduct = await _unitOfWork.ProductsRepository.GetById(@event.ProductId, cancellationToken);

        if (existingProduct != null)
        {
            _logger.LogInformation(
                "Product {ProductId} already exists. Marking event as processed without creating duplicate.",
                @event.ProductId);

            // Mark event as processed
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

        // Create new product
        var product = new Product
        {
            Id = @event.ProductId,
            Name = @event.Name,
            Price = @event.Price,
            Stock = @event.Stock,
            IsActive = true
        };

        _unitOfWork.ProductsRepository.Create(product, cancellationToken);

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
            "Successfully created Product {ProductId} from event {EventId}",
            @event.ProductId,
            @event.EventId);
    }
}
