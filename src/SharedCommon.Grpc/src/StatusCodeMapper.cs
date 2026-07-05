using FluentValidation;
using Grpc.Core;
using SharedCommon.Core.Exceptions;

namespace SharedCommon.Grpc;

/// <summary>Maps exceptions to their appropriate gRPC <see cref="StatusCode"/>.</summary>
public static class StatusCodeMapper
{
    /// <summary>Returns the gRPC <see cref="StatusCode"/> that best represents <paramref name="exception"/>.</summary>
    public static StatusCode Map(Exception exception) => exception switch
    {
        NotFoundException => StatusCode.NotFound,
        ValidationException => StatusCode.InvalidArgument,
        UnauthorizedException => StatusCode.Unauthenticated,
        ForbiddenException => StatusCode.PermissionDenied,
        ConflictException => StatusCode.AlreadyExists,
        TooManyRequestsException => StatusCode.ResourceExhausted,
        TimeoutException => StatusCode.DeadlineExceeded,
        OperationCanceledException => StatusCode.Cancelled,
        _ => StatusCode.Internal
    };
}
