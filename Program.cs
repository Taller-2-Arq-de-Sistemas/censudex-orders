using Microsoft.EntityFrameworkCore;
using CensudexOrders.Behaviors;
using CensudexOrders.Data;
using CensudexOrders.Services;
using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Repositories;
using CensudexOrders.Extensions;
using CensudexOrders.Exceptions;
using CensudexOrders.MessageBroker.Configuration;
using CensudexOrders.MessageBroker.Interfaces;
using CensudexOrders.MessageBroker.Publishers;
using CensudexOrders.MessageBroker.Workers;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Configure RabbitMQ settings
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName));

// Register RabbitMQ services
builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// Register outbox repository
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

// Register processed events repository
builder.Services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

// Register event consumers
builder.Services.AddScoped<CensudexOrders.MessageBroker.Consumers.UserCreatedConsumer>();
builder.Services.AddScoped<CensudexOrders.MessageBroker.Consumers.UserUpdatedConsumer>();
builder.Services.AddScoped<CensudexOrders.MessageBroker.Consumers.UserDeletedConsumer>();
builder.Services.AddScoped<CensudexOrders.MessageBroker.Consumers.ProductCreatedConsumer>();
builder.Services.AddScoped<CensudexOrders.MessageBroker.Consumers.ProductUpdatedConsumer>();
builder.Services.AddScoped<CensudexOrders.MessageBroker.Consumers.ProductDeletedConsumer>();
builder.Services.AddScoped<CensudexOrders.MessageBroker.Consumers.OrderCancelledByInsufficientStockConsumer>();

// Register background workers
builder.Services.AddHostedService<OutboxProcessorWorker>();
builder.Services.AddHostedService<RabbitMqConsumerWorker>();

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    config.AddOpenBehavior(typeof(CommandValidationBehavior<,>));
    config.AddOpenBehavior(typeof(QueryValidationBehavior<,>));
    // NOTE: DomainEventDispatcherBehavior removed - domain events now dispatched in OrdersContext.SaveChangesAsync()
});
builder.Services.AddDbContext<OrdersContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Connection string 'Database' not found."),
        new MySqlServerVersion(new Version(9, 4, 0))
    ));
// Add services to the container.
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GlobalExceptionHandler>();
});
builder.Services.AddSingleton<GlobalExceptionHandler>();
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IOrderProductsRepository, OrderProductsRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<CensudexOrders.Services.Interfaces.IEmailTemplateService, CensudexOrders.Services.EmailTemplateService>();
builder.Services.AddScoped<CensudexOrders.Services.Interfaces.ISendGridService, SendGridService>();

var app = builder.Build();

app.MapGrpcService<OrdersService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
AppSeedService.SeedDatabase(app);

app.Run();
