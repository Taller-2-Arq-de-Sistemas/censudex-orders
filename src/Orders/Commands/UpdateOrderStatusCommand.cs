using CensudexOrders.CQRS;
using CensudexOrders.Exceptions;
using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Services.Interfaces;
using FluentValidation;
using MediatR;

namespace CensudexOrders.Orders.Commands;

public record UpdateOrderStatusCommand(string OrderId, string NewStatus, string UserRole) : ICommand<Unit>;
public class UpdateOrderStatusCommandValidator
: AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.")
            .Must(id => Guid.TryParse(id, out _) == true).WithMessage("OrderId must be a valid GUID.");
        RuleFor(x => x.NewStatus).NotEmpty().WithMessage("NewStatus is required.")
            .Must(status => status.Equals("pendiente", StringComparison.CurrentCultureIgnoreCase) ||
                status.Equals("en procesamiento", StringComparison.CurrentCultureIgnoreCase) ||
                status.Equals("enviado", StringComparison.CurrentCultureIgnoreCase) ||
                status.Equals("entregado", StringComparison.CurrentCultureIgnoreCase) ||
                status.Equals("cancelado", StringComparison.CurrentCultureIgnoreCase))
            .WithMessage("NewStatus must be one of the following values: pendiente, en procesamiento, enviado, entregado, cancelado.");
        RuleFor(x => x.UserRole).NotEmpty().WithMessage("UserRole is required.")
            .Must(role => role == "Admin").WithMessage("Only users with 'Admin' role can update order status.");
    }
}

internal class UpdateOrderStatusCommandHandler(IUnitOfWork unitOfWork, ISendGridService sendGridService)
: ICommandHandler<UpdateOrderStatusCommand, Unit>
{
    public async Task<Unit> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.OrdersRepository.Get(Guid.Parse(request.OrderId), cancellationToken)
            ?? throw new ValidationException($"Order with ID {request.OrderId} does not exist.");

        var newStatus = request.NewStatus.ToLower();
        if (order.Status == newStatus)
            throw new BadRequestException($"The order is already in status '{newStatus}'.");
        order.Status = newStatus;

        unitOfWork.OrdersRepository.Update(order, cancellationToken);
        var user = await unitOfWork.UsersRepository.Get(order.CustomerId, cancellationToken) ??
            throw new ValidationException($"User with ID {order.CustomerId} does not exist.");
        if (newStatus == "cancelado")
        {
            foreach (var item in order.OrderProducts)
            {
                var product = await unitOfWork.ProductsRepository.GetById(item.ProductId, cancellationToken);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                    unitOfWork.ProductsRepository.Update(product, cancellationToken);
                }
            }
            if (request.UserRole == "Admin")
                await sendGridService.SendOrderCancellationAsync(user.Email, user.Name, order.OrderNumber, "An administrator has canceled your order.");
            else
                await sendGridService.SendOrderCancellationAsync(user.Email, user.Name, order.OrderNumber, "You canceled your order.");
        }
        else
            await sendGridService.SendOrderStatusUpdateAsync(user.Email, user.Name, order.OrderNumber, newStatus);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
