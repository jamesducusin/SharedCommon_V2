using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using SharedCommon.Core;

namespace SharedCommon.Grpc;

/// <summary>
/// Server-side interceptor that reads the correlation ID from incoming gRPC metadata and
/// populates <see cref="IRequestContext"/>. Echoes the ID back via trailing metadata.
/// </summary>
public sealed class CorrelationIdInterceptor(
    IRequestContext requestContext,
    IOptions<GrpcOptions> options) : Interceptor
{
    private readonly string _headerKey = options.Value.CorrelationIdHeader;

    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var correlationId = ExtractOrCreate(context.RequestHeaders);
        SetContext(correlationId);
        var response = await continuation(request, context);
        context.ResponseTrailers.Add(_headerKey, correlationId.Value);
        return response;
    }

    /// <inheritdoc />
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var correlationId = ExtractOrCreate(context.RequestHeaders);
        SetContext(correlationId);
        var response = await continuation(requestStream, context);
        context.ResponseTrailers.Add(_headerKey, correlationId.Value);
        return response;
    }

    /// <inheritdoc />
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var correlationId = ExtractOrCreate(context.RequestHeaders);
        SetContext(correlationId);
        await continuation(request, responseStream, context);
        context.ResponseTrailers.Add(_headerKey, correlationId.Value);
    }

    private CorrelationId ExtractOrCreate(Metadata headers)
    {
        var entry = headers.FirstOrDefault(e =>
            string.Equals(e.Key, _headerKey, StringComparison.OrdinalIgnoreCase));

        return CorrelationId.TryCreate(entry?.Value, out var id) ? id! : CorrelationId.New();
    }

    private void SetContext(CorrelationId correlationId)
    {
        if (requestContext is RequestContext rc)
            rc.CorrelationId = correlationId;
    }
}
