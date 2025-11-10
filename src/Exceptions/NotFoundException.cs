using Grpc.Core;

namespace CensudexOrders.Exceptions;

public class NotFoundException : RpcException
{
    public NotFoundException(string message)
        : base(new Status(StatusCode.NotFound, message))
    {
    }

    public NotFoundException(string name, object key)
        : base(new Status(StatusCode.NotFound, $"Entity {name} ({key}) was not found."))
    {
    }
}