using CensudexOrders.CQRS;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;
using FluentValidation;

namespace CensudexOrders.Orders.Commands;

public record CreateOrderCommand(string CustomerId, List<OrderItem> Products) : ICommand<CreateOrderResult>;
public record OrderItem(string ProductId, int Quantity);
public record CreateOrderResult(int Status);
public class CreateOrderCommandValidator
: AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().Must(id => Guid.TryParse(id, out _) == true).WithMessage("CustomerId must be a valid GUID.");
        RuleFor(x => x.Products).NotEmpty();
        RuleForEach(x => x.Products).SetValidator(new OrderItemValidator());
    }

    private class OrderItemValidator : AbstractValidator<OrderItem>
    {
        public OrderItemValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().Must(id => Guid.TryParse(id, out _) == true).WithMessage("ProductId must be a valid GUID.");
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }
}

internal class CreateOrderCommandHandler(IUnitOfWork unitOfWork)
: ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.UsersRepository.Exists(Guid.Parse(request.CustomerId), cancellationToken))
            throw new ValidationException($"Customer with ID {request.CustomerId} does not exist.");

        var orderId = Guid.NewGuid();
        int totalCharge = 0;
        List<OrderProducts> products = new();
        foreach (var item in request.Products)
        {
            var product = await unitOfWork.ProductsRepository.GetById(Guid.Parse(item.ProductId), cancellationToken) ??
                throw new ValidationException($"Product with ID {item.ProductId} does not exist.");
            if (product.Stock < item.Quantity)
                throw new ValidationException($"Insufficient stock for product with ID {item.ProductId}.");
            totalCharge += product.Price * item.Quantity;

            products.Add(new OrderProducts
            {
                OrderId = orderId,
                ProductId = Guid.Parse(item.ProductId),
                Quantity = item.Quantity,
                Price = product.Price * item.Quantity
            });
        }

        Order order = new()
        {
            Id = orderId,
            CustomerId = Guid.Parse(request.CustomerId),
            Status = "Created",
            TotalCharge = totalCharge,
            CreatedAt = DateTime.UtcNow,
        };

        unitOfWork.OrdersRepository.Create(order, cancellationToken);
        unitOfWork.OrderProductsRepository.AddProductsToOrder(products, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateOrderResult(201);
    }
}
