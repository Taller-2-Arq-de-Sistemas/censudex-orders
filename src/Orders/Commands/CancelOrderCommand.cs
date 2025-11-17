using CensudexOrders.CQRS;
using CensudexOrders.Exceptions;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Services.Interfaces;
using FluentValidation;
using Google.Type;
using MediatR;
using Microsoft.AspNetCore.Http.Features;

namespace CensudexOrders.Orders.Commands;

public record CancelOrderCommand(string OrderId, string CustomerId, string CustomerRole) : ICommand<Unit>;
public class CancelOrderCommandValidator
: AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.")
            .Must(id => Guid.TryParse(id, out _) == true).WithMessage("OrderId must be a valid GUID.");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId is required.")
            .Must(id => Guid.TryParse(id, out _) == true).WithMessage("CustomerId must be a valid GUID.");
        RuleFor(x => x.CustomerRole).NotEmpty().WithMessage("CustomerRole is required.")
            .Must(role => role == "Admin" || role == "User").WithMessage("CustomerRole must be either 'Admin' or 'User'.");
    }
}

internal class CancelOrderCommandHandler(IUnitOfWork unitOfWork, ISendGridService sendGridService)
: ICommandHandler<CancelOrderCommand, Unit>
{
    public async Task<Unit> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.OrdersRepository.Get(Guid.Parse(request.OrderId), cancellationToken)
            ?? throw new ValidationException($"Order with ID {request.OrderId} does not exist.");

        var user = await unitOfWork.UsersRepository.Get(order.CustomerId, cancellationToken) ??
            throw new ValidationException($"User with ID {order.CustomerId} does not exist.");

        if (request.CustomerRole != "Admin" && order.CustomerId != Guid.Parse(request.CustomerId))
            throw new UnauthorizedAccessException("You do not have permission to cancel this order.");

        if (order.Status == "cancelado")
            throw new BadRequestException("The order is already canceled.");

        if (order.Status == "entregado")
            throw new BadRequestException("Delivered orders cannot be canceled.");

        if (request.CustomerRole == "User" && System.DateTime.UtcNow - order.CreatedAt.ToUniversalTime() > TimeSpan.FromHours(48))
            throw new BadRequestException("You can only cancel orders within 48 hours of creation.");

        order.Status = "cancelado";
        unitOfWork.OrdersRepository.Update(order, cancellationToken);
        foreach (var item in order.OrderProducts)
        {
            var product = await unitOfWork.ProductsRepository.GetById(item.ProductId, cancellationToken);
            if (product != null)
            {
                product.Stock += item.Quantity;
                unitOfWork.ProductsRepository.Update(product, cancellationToken);
            }
        }
        await sendGridService.SendOrderCancellationAsync(user.Email, user.Name, order.OrderNumber,
            request.CustomerRole == "Admin" ? "An administrator has canceled your order." : "You canceled your order.");
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
