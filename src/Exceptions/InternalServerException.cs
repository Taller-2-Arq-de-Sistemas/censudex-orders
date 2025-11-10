using Grpc.Core;

namespace CensudexOrders.Exceptions;

public class InternalServerException : RpcException
{
    public InternalServerException(string message)
        : base(new Status(StatusCode.Internal, message))
    {
    }

    public InternalServerException(string message, string details)
        : base(new Status(StatusCode.Internal, message))
    {
        Details = details;
    }

    public string? Details { get; }
}
