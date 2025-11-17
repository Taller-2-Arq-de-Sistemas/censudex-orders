using FluentValidation;
using CensudexOrders.CQRS;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Exceptions;

namespace CensudexOrders.Orders.Queries;

public record GetOrderByNumberQuery(int OrderNumber, string CustomerId, string CustomerRole) : IQuery<GetOrderByNumberResult>;
public record GetOrderByNumberResult(Order Order);

public class GetOrderByNumberQueryValidator
: AbstractValidator<GetOrderByNumberQuery>
{
    public GetOrderByNumberQueryValidator()
    {
        RuleFor(x => x.OrderNumber).NotEmpty().WithMessage("OrderNumber is required.")
            .Must(orderNumber => int.TryParse(orderNumber.ToString(), out _)).WithMessage("OrderNumber must be a valid integer.")
            .GreaterThan(0).WithMessage("OrderNumber is a positive integer.");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId is required.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("CustomerId must be a valid GUID.");
        RuleFor(x => x.CustomerRole).NotEmpty().WithMessage("CustomerRole is required.")
            .Must(role => role == "Admin" || role == "User").WithMessage("CustomerRole must be either 'Admin' or 'User'.");
    }
}

internal class GetOrderByNumberQueryHandler(IUnitOfWork unitOfWork)
: IQueryHandler<GetOrderByNumberQuery, GetOrderByNumberResult>
{
    public async Task<GetOrderByNumberResult> Handle(GetOrderByNumberQuery request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.OrdersRepository.Get(request.OrderNumber, cancellationToken)
            ?? throw new NotFoundException($"Order with number {request.OrderNumber} not found.");

        if (request.CustomerRole != "Admin" && order.CustomerId != Guid.Parse(request.CustomerId))
            throw new UnauthorizedException("You do not have permission to access this order.");

        return new GetOrderByNumberResult(order);
    }
}
