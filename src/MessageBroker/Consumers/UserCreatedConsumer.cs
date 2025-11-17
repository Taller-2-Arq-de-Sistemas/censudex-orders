using CensudexOrders.Events.Integration.Consumed;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.MessageBroker.Consumers;

/// <summary>
/// Consumes UserCreated events from RabbitMQ and creates users in the local database
/// </summary>
public class UserCreatedConsumer : IEventConsumer<UserCreatedIntegrationEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(
        IUnitOfWork unitOfWork,
        ILogger<UserCreatedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing UserCreated event {EventId} for User {UserId}",
            @event.EventId,
            @event.UserId);

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

        // Check if user already exists (could be from manual seeding)
        var existingUser = await _unitOfWork.UsersRepository.Get(@event.UserId, cancellationToken);

        if (existingUser != null)
        {
            _logger.LogInformation(
                "User {UserId} already exists. Marking event as processed without creating duplicate.",
                @event.UserId);

            // Mark event as processed
            var processedEvent = new ProcessedEvent
            {
                Id = eventId,
                EventType = @event.EventType,
                ProcessedAt = DateTime.UtcNow,
                SourceService = "users-service"
            };
            await _unitOfWork.ProcessedEventsRepository.AddAsync(processedEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        // Create new user
        var user = new User
        {
            Id = @event.UserId,
            Name = @event.Name,
            LastNames = @event.LastNames,
            Address = @event.Address,
            Email = @event.Email,
            IsActive = true
        };

        _unitOfWork.UsersRepository.Create(user, cancellationToken);

        // Mark event as processed
        var processed = new ProcessedEvent
        {
            Id = eventId,
            EventType = @event.EventType,
            ProcessedAt = DateTime.UtcNow,
            SourceService = "users-service"
        };
        await _unitOfWork.ProcessedEventsRepository.AddAsync(processed, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully created User {UserId} from event {EventId}",
            @event.UserId,
            @event.EventId);
    }
}
