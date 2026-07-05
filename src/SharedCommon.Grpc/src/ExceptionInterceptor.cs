using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace SharedCommon.Grpc;

/// <summary>
/// Server-side interceptor that catches unhandled exceptions and maps them to appropriate
/// gRPC <see cref="StatusCode"/> values. Stack traces are never exposed in status detail.
/// </summary>
public sealed class ExceptionInterceptor(ILogger<ExceptionInterceptor> logger) : Interceptor
{
    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw MapToRpcException(ex);
        }
    }

    /// <inheritdoc />
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw MapToRpcException(ex);
        }
    }

    /// <inheritdoc />
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw MapToRpcException(ex);
        }
    }

    private RpcException MapToRpcException(Exception exception)
    {
        var statusCode = StatusCodeMapper.Map(exception);
        // Never expose internal details in the gRPC status message
        var detail = statusCode == StatusCode.Internal
            ? "An internal error occurred."
            : exception.Message;

        logger.LogWarning(exception, "gRPC exception mapped to {StatusCode}: {Message}", statusCode, detail);
        return new RpcException(new Status(statusCode, detail));
    }
}
