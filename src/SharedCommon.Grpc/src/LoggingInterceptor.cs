using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using SharedCommon.Core;

namespace SharedCommon.Grpc;

/// <summary>
/// Server-side interceptor that emits structured log entries for every gRPC call:
/// one at start and one on completion (success or failure) with elapsed time.
/// </summary>
public sealed class LoggingInterceptor(
    ILogger<LoggingInterceptor> logger,
    IRequestContext requestContext) : Interceptor
{
    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var start = Stopwatch.GetTimestamp();
        LogStart(context.Method);
        try
        {
            var response = await continuation(request, context);
            LogSuccess(context.Method, Stopwatch.GetElapsedTime(start));
            return response;
        }
        catch
        {
            LogFailure(context.Method, Stopwatch.GetElapsedTime(start));
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var start = Stopwatch.GetTimestamp();
        LogStart(context.Method);
        try
        {
            var response = await continuation(requestStream, context);
            LogSuccess(context.Method, Stopwatch.GetElapsedTime(start));
            return response;
        }
        catch
        {
            LogFailure(context.Method, Stopwatch.GetElapsedTime(start));
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var start = Stopwatch.GetTimestamp();
        LogStart(context.Method);
        try
        {
            await continuation(request, responseStream, context);
            LogSuccess(context.Method, Stopwatch.GetElapsedTime(start));
        }
        catch
        {
            LogFailure(context.Method, Stopwatch.GetElapsedTime(start));
            throw;
        }
    }

    private void LogStart(string method) =>
        logger.LogInformation("gRPC call started {Method} {CorrelationId}",
            method, requestContext.CorrelationId.Value);

    private void LogSuccess(string method, TimeSpan elapsed) =>
        logger.LogInformation("gRPC call completed {Method} in {ElapsedMs}ms {CorrelationId}",
            method, elapsed.TotalMilliseconds, requestContext.CorrelationId.Value);

    private void LogFailure(string method, TimeSpan elapsed) =>
        logger.LogWarning("gRPC call failed {Method} after {ElapsedMs}ms {CorrelationId}",
            method, elapsed.TotalMilliseconds, requestContext.CorrelationId.Value);
}
