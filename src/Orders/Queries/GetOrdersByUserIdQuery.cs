using FluentValidation;
using CensudexOrders.CQRS;
using CensudexOrders.Models;
using CensudexOrders.Repositories.Interfaces;
using CensudexOrders.Exceptions;

namespace CensudexOrders.Orders.Queries;

public record GetOrdersByUserIdQuery(string UserId) : IQuery<GetOrdersByUserIdResult>;
public record GetOrdersByUserIdResult(List<Order> Orders);

public class GetOrdersByUserIdQueryValidator
: AbstractValidator<GetOrdersByUserIdQuery>
{
    public GetOrdersByUserIdQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("UserId must be a valid GUID.");
    }
}

internal class GetOrdersByUserIdQueryHandler(IUnitOfWork unitOfWork)
: IQueryHandler<GetOrdersByUserIdQuery, GetOrdersByUserIdResult>
{
    public async Task<GetOrdersByUserIdResult> Handle(GetOrdersByUserIdQuery request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.UsersRepository.Exists(Guid.Parse(request.UserId), cancellationToken) == false)
            throw new NotFoundException($"User with ID {request.UserId} not found.");
        var orders = await unitOfWork.OrdersRepository.GetByUserId(Guid.Parse(request.UserId), cancellationToken);
        return new GetOrdersByUserIdResult(orders);
    }
}
