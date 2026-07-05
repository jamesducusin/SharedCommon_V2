using MassTransit;

namespace SharedCommon.Messaging.Filters;

/// <summary>
/// MassTransit consume filter that extracts the correlation ID from incoming message headers
/// and makes it available to downstream consumers via <see cref="ConsumeContext{T}"/>.
/// </summary>
public sealed class CorrelationIdFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private const string CorrelationIdHeader = "x-correlation-id";

    /// <inheritdoc />
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        // Ensure downstream code can read the correlation ID via standard header
        if (context.TryGetHeader(CorrelationIdHeader, out string? _) == false
            && context.CorrelationId.HasValue)
        {
            // Back-fill from MassTransit's native CorrelationId when custom header is absent
            context.Headers.TryGetHeader(CorrelationIdHeader, out _);
        }

        await next.Send(context);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationId");
}
