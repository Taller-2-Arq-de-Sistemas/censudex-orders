using Grpc.Core;

namespace CensudexOrders.Exceptions;

public class BadRequestException : RpcException
{
    public BadRequestException(string message)
        : base(new Status(StatusCode.InvalidArgument, message))
    {
    }
    public BadRequestException(string message, string details)
        : base(new Status(StatusCode.InvalidArgument, message))
    {
        Details = details;
    }
    public string? Details { get; }
}