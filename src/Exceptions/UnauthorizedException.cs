using Grpc.Core;

namespace CensudexOrders.Exceptions;

public class UnauthorizedException : RpcException
{
    public UnauthorizedException(string message)
        : base(new Status(StatusCode.PermissionDenied, message))
    {
    }
    public UnauthorizedException(string message, string details)
        : base(new Status(StatusCode.PermissionDenied, message))
    {
        Details = details;
    }
    public string? Details { get; }
}