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

    /// <summary>
    /// Maps an Order entity to a protobuf Order message including products
    /// </summary>
    private static Order MapOrderToProto(Models.Order order)
    {
        var protoOrder = order.Adapt<Order>();

        foreach (var orderProduct in order.OrderProducts)
        {
            protoOrder.OrderProducts.Add(new OrderProduct
            {
                ProductId = orderProduct.ProductId.ToString(),
                Quantity = orderProduct.Quantity,
                Price = orderProduct.Price
            });
        }

        return protoOrder;
    }
    public override async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        var command = request.Adapt<CreateOrderCommand>();
        var result = await sender.Send(command, context.CancellationToken);

        return new CreateOrderResponse
        {
            Order = MapOrderToProto(result.Order)
        };
    }

    public override async Task<GetOrderByIdResponse> GetOrderById(GetOrderByIdRequest request, ServerCallContext context)
    {
        var query = request.Adapt<GetOrderByIdQuery>();
        var result = await sender.Send(query, context.CancellationToken);

        return new GetOrderByIdResponse
        {
            Order = MapOrderToProto(result.Order)
        };
    }

    public override async Task<GetOrderByNumberResponse> GetOrderByNumber(GetOrderByNumberRequest request, ServerCallContext context)
    {
        var query = request.Adapt<GetOrderByNumberQuery>();
        var result = await sender.Send(query, context.CancellationToken);

        return new GetOrderByNumberResponse
        {
            Order = MapOrderToProto(result.Order)
        };
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> UpdateOrderStatus(UpdateOrderStatusRequest request, ServerCallContext context)
    {
        var command = request.Adapt<UpdateOrderStatusCommand>();
        await sender.Send(command, context.CancellationToken);
        return new Google.Protobuf.WellKnownTypes.Empty();
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> CancelOrder(CancelOrderRequest request, ServerCallContext context)
    {
        var command = request.Adapt<CancelOrderCommand>();
        await sender.Send(command, context.CancellationToken);
        return new Google.Protobuf.WellKnownTypes.Empty();
    }

    public override async Task<GetOrdersByUserIdResponse> GetOrdersByUserId(GetOrdersByUserIdRequest request, ServerCallContext context)
    {
        var query = request.Adapt<GetOrdersByUserIdQuery>();
        var result = await sender.Send(query, context.CancellationToken);

        var response = new GetOrdersByUserIdResponse();
        foreach (var order in result.Orders)
        {
            response.Orders.Add(MapOrderToProto(order));
        }

        return response;
    }
}
