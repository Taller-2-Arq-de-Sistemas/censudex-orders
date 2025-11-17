using CensudexOrders.Events.Integration.Consumed;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;

namespace CensudexOrders.MessageBroker.Consumers;

/// <summary>
/// Consumes UserUpdated events from RabbitMQ and updates users in the local database
/// </summary>
public class UserUpdatedConsumer : IEventConsumer<UserUpdatedIntegrationEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserUpdatedConsumer> _logger;

    public UserUpdatedConsumer(
        IUnitOfWork unitOfWork,
        ILogger<UserUpdatedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(UserUpdatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing UserUpdated event {EventId} for User {UserId}",
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
                "User {UserId} not found. Cannot update. Creating new user instead.",
                @event.UserId);

            // Create user if doesn't exist (out-of-order event handling)
            user = new User
            {
                Id = @event.UserId,
                Name = @event.Name,
                LastNames = @event.LastNames,
                Address = @event.Address,
                Email = @event.Email,
                IsActive = true
            };

            _unitOfWork.UsersRepository.Create(user, cancellationToken);
        }
        else
        {
            // Update existing user
            user.Name = @event.Name;
            user.LastNames = @event.LastNames;
            user.Address = @event.Address;
            user.Email = @event.Email;

            _unitOfWork.UsersRepository.Update(user, cancellationToken);
        }

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
            "Successfully updated User {UserId} from event {EventId}",
            @event.UserId,
            @event.EventId);
    }
}
