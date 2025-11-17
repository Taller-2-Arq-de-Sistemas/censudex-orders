using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CensudexOrders.Exceptions;

public sealed class GlobalExceptionHandler : Interceptor
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception ex)
        {
            throw Handle(ex);
        }
    }

    private RpcException Handle(Exception exception)
    {
        _logger.LogError(
            "Error message: {exceptionMessage}, Time of occurrence: {time}",
            exception.Message,
            DateTime.UtcNow
        );

        // Handle ValidationException with field violations
        if (exception is ValidationException validationException)
        {
            var status = new Google.Rpc.Status
            {
                Code = (int)Code.InvalidArgument,
                Message = "Validation errors occurred",
                Details =
                {
                    Any.Pack(new BadRequest
                    {
                        FieldViolations =
                        {
                            validationException.Errors.Select(error => new BadRequest.Types.FieldViolation
                            {
                                Field = error.PropertyName,
                                Description = error.ErrorMessage
                            })
                        }
                    }),
                    Any.Pack(new ErrorInfo
                    {
                        Reason = "VALIDATION_ERROR",
                        Domain = "CensudexOrders",
                        Metadata =
                        {
                            { "Timestamp", DateTime.UtcNow.ToString("o") }
                        }
                    })
                }
            };
            return status.ToRpcException();
        }

        // Handle other exceptions
        (string Detail, string Title, int grpcStatusCode) = exception switch
        {
            InternalServerException =>
            (
                exception.Message,
                exception.GetType().Name,
                (int)Code.Internal
            ),
            BadRequestException =>
            (
                exception.Message,
                exception.GetType().Name,
                (int)Code.InvalidArgument
            ),
            NotFoundException =>
            (
                exception.Message,
                exception.GetType().Name,
                (int)Code.NotFound
            ),
            UnauthorizedException =>
            (
                exception.Message,
                exception.GetType().Name,
                (int)Code.PermissionDenied
            ),
            _ =>
            (
                exception.Message,
                exception.GetType().Name,
                (int)Code.Unknown
            )
        };

        var problemDetails = new Google.Rpc.Status
        {
            Code = grpcStatusCode,
            Message = Title,
            Details =
            {
                Any.Pack(new ErrorInfo
                {
                    Reason = Title,
                    Domain = "CensudexOrders",
                    Metadata =
                    {
                        { "Timestamp", DateTime.UtcNow.ToString("o") },
                        { "Detail", Detail },
                        { "Title", Title },
                    }
                })
            }
        };

        return problemDetails.ToRpcException();
    }
}