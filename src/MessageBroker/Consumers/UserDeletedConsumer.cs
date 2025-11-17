using CensudexOrders.Events.Integration.Consumed;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.MessageBroker.Consumers;

/// <summary>
/// Consumes UserDeleted events from RabbitMQ and soft-deletes users in the local database
/// </summary>
public class UserDeletedConsumer : IEventConsumer<UserDeletedIntegrationEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserDeletedConsumer> _logger;

    public UserDeletedConsumer(
        IUnitOfWork unitOfWork,
        ILogger<UserDeletedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(UserDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing UserDeleted event {EventId} for User {UserId}",
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

        // Get existing user
        var user = await _unitOfWork.UsersRepository.Get(@event.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning(
                "User {UserId} not found. Cannot soft-delete.",
                @event.UserId);

            // Still mark event as processed to maintain idempotency
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

        // Soft-delete user by setting IsActive to false
        user.IsActive = false;
        _unitOfWork.UsersRepository.Update(user, cancellationToken);

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
            "Successfully soft-deleted User {UserId} from event {EventId}",
            @event.UserId,
            @event.EventId);
    }
}
