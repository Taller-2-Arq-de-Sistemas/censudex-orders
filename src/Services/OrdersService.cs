using Grpc.Core;
using Mapster;
using CensudexOrders.Orders.Commands;
using CensudexOrders.Orders.Queries;
using CensudexOrders.Protos;
using MediatR;

namespace CensudexOrders.Services;

public class OrdersService(ISender sender) : OrdersProtoService.OrdersProtoServiceBase
{
    private readonly ISender sender = sender;
    public override async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        var command = request.Adapt<CreateOrderCommand>();
        var result = await sender.Send(command, context.CancellationToken);
        var response = result.Adapt<CreateOrderResponse>();
        return response;
    }

    public override async Task<GetOrdersByUserIdResponse> GetOrdersByUserId(GetOrdersByUserIdRequest request, ServerCallContext context)
    {
        var query = request.Adapt<GetOrdersByUserIdQuery>();
        var result = await sender.Send(query, context.CancellationToken);
        var response = result.Adapt<GetOrdersByUserIdResponse>();
        return response;
    }
}
