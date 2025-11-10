using Microsoft.EntityFrameworkCore;
using CensudexOrders.Behaviors;
using CensudexOrders.Data;
using CensudexOrders.Services;
using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Repositories;
using CensudexOrders.Extensions;
using CensudexOrders.Exceptions;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
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

var app = builder.Build();

app.MapGrpcService<OrdersService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
AppSeedService.SeedDatabase(app);

app.Run();
