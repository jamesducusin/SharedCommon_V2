using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SharedCommon.Observability;

/// <summary>DI registration extensions for SharedCommon OpenTelemetry infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing and metrics with SharedCommon ActivitySources, Meters,
    /// W3C + correlation ID propagation, and optional OTLP export.
    /// Configuration is read from <c>SharedCommon:Observability</c>.
    /// </summary>
    public static IServiceCollection AddSharedObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ObservabilityOptions>()
            .BindConfiguration(ObservabilityOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Eagerly read options — needed before DI is built
        var options = configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions { ServiceName = "unknown" };

        // Install composite propagator globally
        Sdk.SetDefaultTextMapPropagator(new CorrelationPropagator());

        var resource = ResourceBuilder.CreateDefault()
            .AddService(options.ServiceName, serviceVersion: options.ServiceVersion);

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resource);
                tracing.SetSampler(new TraceIdRatioBasedSampler(options.SamplingRatio));

                // SharedCommon activity sources
                foreach (var name in ActivitySources.AllSourceNames)
                    tracing.AddSource(name);

                if (options.InstrumentAspNetCore)
                    tracing.AddAspNetCoreInstrumentation(o =>
                    {
                        // Always record exceptions regardless of sampling ratio
                        o.RecordException = true;
                    });

                if (options.InstrumentHttpClient)
                    tracing.AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resource);

                foreach (var name in Meters.AllMeterNames)
                    metrics.AddMeter(name);

                if (options.InstrumentAspNetCore)
                    metrics.AddAspNetCoreInstrumentation();

                if (options.InstrumentHttpClient)
                    metrics.AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
            });

        return services;
    }
}
