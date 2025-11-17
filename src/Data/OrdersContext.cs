using Microsoft.EntityFrameworkCore;
using MediatR;
using CensudexOrders.Models;

namespace CensudexOrders.Data;

public class OrdersContext : DbContext
{
    private readonly IMediator? _mediator;

    public DbSet<Order> Orders { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<OrderProducts> OrderProducts { get; set; } = default!;
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = default!;
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; } = default!;

    public OrdersContext(DbContextOptions<OrdersContext> options) : base(options)
    {
    }

    public OrdersContext(DbContextOptions<OrdersContext> options, IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .Property(o => o.OrderNumber)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Order>()
            .HasAlternateKey(o => o.OrderNumber);

        // Outbox message configuration
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Payload)
                .IsRequired()
                .HasColumnType("json");

            entity.Property(e => e.Error)
                .HasMaxLength(2000);

            // Index for querying unpublished messages
            entity.HasIndex(e => new { e.PublishedAt, e.CreatedAt })
                .HasDatabaseName("IX_OutboxMessages_PublishedAt_CreatedAt");

            // Index for cleanup queries
            entity.HasIndex(e => e.PublishedAt)
                .HasDatabaseName("IX_OutboxMessages_PublishedAt");
        });

        // Processed events configuration
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.SourceService)
                .IsRequired()
                .HasMaxLength(100);

            // Composite index for idempotency checks
            entity.HasIndex(e => new { e.EventType, e.Id })
                .HasDatabaseName("IX_ProcessedEvents_EventType_Id");

            // Index for cleanup queries
            entity.HasIndex(e => e.ProcessedAt)
                .HasDatabaseName("IX_ProcessedEvents_ProcessedAt");
        });
    }

    /// <summary>
    /// Override SaveChangesAsync to dispatch domain events BEFORE saving to ensure
    /// Order and OutboxMessages are saved in the same transaction
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        if (_mediator != null)
        {
            await DispatchDomainEventsAsync(cancellationToken);
        }

        // Now save everything (Order + OutboxMessages) in one transaction
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Get all entities that have domain events
        var entitiesWithEvents = ChangeTracker.Entries<EntityBase>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (!entitiesWithEvents.Any())
        {
            return;
        }

        // Collect all domain events
        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear domain events from entities
        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        // Dispatch each domain event
        foreach (var domainEvent in domainEvents)
        {
            await _mediator!.Publish(domainEvent, cancellationToken);
        }
    }
}