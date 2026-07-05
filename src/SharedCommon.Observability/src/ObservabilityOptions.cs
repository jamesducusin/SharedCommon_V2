using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Observability;

/// <summary>Configuration for OpenTelemetry tracing and metrics export.</summary>
public sealed class ObservabilityOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:Observability";

    /// <summary>Logical service name reported in all traces and metrics. Required.</summary>
    [Required]
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>Service version, typically the assembly version.</summary>
    public string ServiceVersion { get; init; } = "1.0.0";

    /// <summary>OTLP exporter endpoint (e.g., http://otel-collector:4317). If null, export is disabled.</summary>
    public string? OtlpEndpoint { get; init; }

    /// <summary>Probability sampler rate: 1.0 = always sample, 0.1 = sample 10%. Always samples errors.</summary>
    [Range(0.0, 1.0)]
    public double SamplingRatio { get; init; } = 1.0;

    /// <summary>Whether to enable ASP.NET Core HTTP request instrumentation.</summary>
    public bool InstrumentAspNetCore { get; init; } = true;

    /// <summary>Whether to enable outbound HttpClient instrumentation.</summary>
    public bool InstrumentHttpClient { get; init; } = true;
}
