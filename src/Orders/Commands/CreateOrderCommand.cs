using CensudexOrders.CQRS;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Services.Interfaces;
using FluentValidation;

namespace CensudexOrders.Orders.Commands;

public record CreateOrderCommand(string CustomerId, List<OrderItem> Products) : ICommand<CreateOrderResult>;
public record OrderItem(string ProductId, int Quantity);
public record CreateOrderResult(Order Order);
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

internal class CreateOrderCommandHandler(IUnitOfWork unitOfWork, ISendGridService sendGridService)
: ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.UsersRepository.Get(Guid.Parse(request.CustomerId), cancellationToken) ??
            throw new ValidationException($"Customer with ID {request.CustomerId} does not exist.");
        if (!customer.IsActive)
            throw new ValidationException($"Customer with ID {request.CustomerId} is no longer active.");

        var orderId = Guid.NewGuid();
        int totalCharge = 0;
        List<OrderProducts> products = new();
        foreach (var item in request.Products)
        {
            var product = await unitOfWork.ProductsRepository.GetById(Guid.Parse(item.ProductId), cancellationToken) ??
                throw new ValidationException($"Product with ID {item.ProductId} does not exist.");
            if (!product.IsActive)
                throw new ValidationException($"Product with ID {item.ProductId} is no longer available.");
            if (product.Stock < item.Quantity)
                throw new ValidationException($"Insufficient stock for product with ID {item.ProductId}.");
            totalCharge += product.Price * item.Quantity;
            product.Stock -= item.Quantity;
            unitOfWork.ProductsRepository.Update(product, cancellationToken);

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
            Status = "pendiente",
            TotalCharge = totalCharge,
            CreatedAt = DateTime.UtcNow,
        };

        unitOfWork.OrdersRepository.Create(order, cancellationToken);
        unitOfWork.OrderProductsRepository.AddProductsToOrder(products, cancellationToken);

        order.RaiseOrderCreatedEvent();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sendGridService.SendOrderConfirmationAsync(
            customer.Email,
            customer.Name,
            order.OrderNumber,
            order.CreatedAt.ToString("yyyy-MM-dd"),
            order.Status,
            order.TotalCharge
        );

        return new CreateOrderResult(order);
    }
}
