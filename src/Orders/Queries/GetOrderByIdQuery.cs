using FluentValidation;
using CensudexOrders.CQRS;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Exceptions;

namespace CensudexOrders.Orders.Queries;

public record GetOrderByIdQuery(string OrderId, string CustomerId, string CustomerRole) : IQuery<GetOrderByIdResult>;
public record GetOrderByIdResult(Order Order);

public class GetOrderByIdQueryValidator
: AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("OrderId must be a valid GUID.");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId is required.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("CustomerId must be a valid GUID.");
        RuleFor(x => x.CustomerRole).NotEmpty().WithMessage("CustomerRole is required.")
            .Must(role => role == "Admin" || role == "User").WithMessage("CustomerRole must be either 'Admin' or 'User'.");
    }
}

internal class GetOrderByIdQueryHandler(IUnitOfWork unitOfWork)
: IQueryHandler<GetOrderByIdQuery, GetOrderByIdResult>
{
    public async Task<GetOrderByIdResult> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.OrdersRepository.Get(Guid.Parse(request.OrderId), cancellationToken)
            ?? throw new NotFoundException($"Order with ID {request.OrderId} not found.");

        if (request.CustomerRole != "Admin" && order.CustomerId != Guid.Parse(request.CustomerId))
            throw new UnauthorizedException("You do not have permission to access this order.");

        return new GetOrderByIdResult(order);
    }
}
