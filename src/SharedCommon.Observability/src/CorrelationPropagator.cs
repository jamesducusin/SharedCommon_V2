using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace SharedCommon.Observability;

/// <summary>
/// Composite text-map propagator that chains W3C TraceContext, W3C Baggage, and
/// the custom <c>x-correlation-id</c> header so correlation IDs flow through
/// all outgoing calls alongside standard OpenTelemetry trace context.
/// </summary>
public sealed class CorrelationPropagator : TextMapPropagator
{
    private const string CorrelationIdHeader = "x-correlation-id";
    private const string CorrelationIdBaggageKey = "correlation.id";

    private static readonly TextMapPropagator _composite =
        new CompositeTextMapPropagator([
            new TraceContextPropagator(),
            new BaggagePropagator()
        ]);

    /// <inheritdoc />
    public override ISet<string> Fields =>
        new HashSet<string>(_composite.Fields!) { CorrelationIdHeader };

    /// <inheritdoc />
    public override PropagationContext Extract<T>(
        PropagationContext context,
        T carrier,
        Func<T, string, IEnumerable<string>?> getter)
    {
        var propagated = _composite.Extract(context, carrier, getter);

        // Extract custom correlation ID header and store in Baggage
        var correlationId = getter(carrier, CorrelationIdHeader)?.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            var baggage = propagated.Baggage.SetBaggage(CorrelationIdBaggageKey, correlationId);
            propagated = new PropagationContext(propagated.ActivityContext, baggage);
        }

        return propagated;
    }

    /// <inheritdoc />
    public override void Inject<T>(
        PropagationContext context,
        T carrier,
        Action<T, string, string> setter)
    {
        _composite.Inject(context, carrier, setter);

        // Inject the correlation ID from Baggage as the custom header
        var correlationId = context.Baggage.GetBaggage(CorrelationIdBaggageKey);
        if (!string.IsNullOrWhiteSpace(correlationId))
            setter(carrier, CorrelationIdHeader, correlationId);
    }
}
