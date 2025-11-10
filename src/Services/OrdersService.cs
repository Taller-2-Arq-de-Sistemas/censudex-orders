using Grpc.Core;
using Mapster;
using CensudexOrders.Orders.Commands;
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
}
